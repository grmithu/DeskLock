using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeskLock;

namespace DeskLock.Services;

/// <summary>Opt-in process diagnostics. Sampling and report I/O run outside the UI thread.</summary>
public sealed class MemoryDiagnosticsService : IDisposable
{
    public const int DefaultIntervalSeconds = 5;
    public const int MinimumIntervalSeconds = 1;
    public const int MaximumIntervalSeconds = 3600;

    private const string CsvHeader =
        "TimestampUtc,State,WorkingSetBytes,ManagedHeapBytes,Gen0Count,Gen1Count,Gen2Count,Gen0Delta,Gen1Delta,Gen2Delta,CpuPercent";

    private readonly object _ioLock = new();
    private readonly string _diagnosticsDirectory;
    private readonly int _intervalSeconds;
    private readonly int _logicalProcessorCount;
    private readonly DateTimeOffset _sessionStartUtc;
    private readonly StringBuilder _csvLineBuilder = new(192);
    private readonly StateSummary _idleSummary = new();
    private readonly StateSummary _activeSummary = new();

    private string? _csvPath;
    private string? _summaryPath;
    private StreamWriter? _csvWriter;
    private Process? _process;
    private Timer? _timer;
    private Task? _initializationTask;
    private long _previousTimestamp;
    private TimeSpan _previousCpuTime;
    private int _previousGen0Count;
    private int _previousGen1Count;
    private int _previousGen2Count;
    private int _activitySincePreviousSample;
    private int _sampling;
    private int _stopping;
    private int _faulted;
    private int _started;
    private int _finalized;

    private MemoryDiagnosticsService(int intervalSeconds)
    {
        if (intervalSeconds is < MinimumIntervalSeconds or > MaximumIntervalSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
        }

        _intervalSeconds = intervalSeconds;
        _logicalProcessorCount = Math.Max(1, Environment.ProcessorCount);
        _sessionStartUtc = DateTimeOffset.UtcNow;
        _diagnosticsDirectory = Path.Combine(AppSettings.ImageFolder, "Diagnostics");
    }

    public static bool TryStart(int intervalSeconds, out MemoryDiagnosticsService? service)
    {
        service = null;
        MemoryDiagnosticsService? candidate = null;

        try
        {
            candidate = new MemoryDiagnosticsService(intervalSeconds);
            candidate.BeginStart();
            service = candidate;
            return true;
        }
        catch (Exception ex)
        {
            try { candidate?.Dispose(); } catch { }
            SafeTraceWarning("Could not start diagnostics. DeskLock will continue without them.", ex);
            return false;
        }
    }

    public void MarkActivity()
    {
        if (Volatile.Read(ref _stopping) == 0 && Volatile.Read(ref _faulted) == 0)
        {
            Interlocked.Exchange(ref _activitySincePreviousSample, 1);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _stopping, 1) != 0)
        {
            return;
        }

        try { _initializationTask?.GetAwaiter().GetResult(); }
        catch (Exception ex) { SafeTraceWarning("Could not stop diagnostics initialization cleanly.", ex); }

        var timer = Interlocked.Exchange(ref _timer, null);
        if (timer is not null)
        {
            try { timer.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
            try { timer.Dispose(); } catch { }
        }

        // Wait for any sampler already writing. Queued callbacks check _stopping under this lock.
        try
        {
            lock (_ioLock) { }
            Task.Run(FinalizeResources).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            SafeTraceWarning("Could not finish diagnostics cleanly.", ex);
            FinalizeResources();
        }
    }

    private void BeginStart()
    {
        _initializationTask = Task.Run(Initialize);
    }

    private void Initialize()
    {
        try
        {
            if (Volatile.Read(ref _stopping) != 0)
            {
                return;
            }

            Directory.CreateDirectory(_diagnosticsDirectory);

            var stamp = _sessionStartUtc.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
            var fileStem = $"memory-diagnostics-{stamp}-{Guid.NewGuid():N}";
            _csvPath = Path.Combine(_diagnosticsDirectory, fileStem + ".csv");
            _summaryPath = Path.Combine(_diagnosticsDirectory, fileStem + ".txt");

            _process = Process.GetCurrentProcess();
            _process.Refresh();
            _previousCpuTime = _process.TotalProcessorTime;
            _previousTimestamp = Stopwatch.GetTimestamp();
            _previousGen0Count = GC.CollectionCount(0);
            _previousGen1Count = GC.CollectionCount(1);
            _previousGen2Count = GC.CollectionCount(2);

            if (Volatile.Read(ref _stopping) != 0)
            {
                return;
            }

            var stream = new FileStream(_csvPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            try
            {
                _csvWriter = new StreamWriter(stream, new UTF8Encoding(false));
            }
            catch
            {
                stream.Dispose();
                throw;
            }

            _csvWriter.WriteLine(CsvHeader);
            if (Volatile.Read(ref _stopping) != 0)
            {
                return;
            }

            _timer = new Timer(TimerTick, null, Timeout.Infinite, Timeout.Infinite);
            if (Volatile.Read(ref _stopping) != 0)
            {
                _timer.Dispose();
                _timer = null;
                return;
            }

            Volatile.Write(ref _started, 1);
            _timer.Change(_intervalSeconds * 1000, _intervalSeconds * 1000);
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _faulted, 1);
            try { _timer?.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
            SafeTraceWarning("Could not initialize diagnostics. DeskLock will continue without them.", ex);
            FinalizeResources();
        }
    }

    private void TimerTick(object? state)
    {
        if (Volatile.Read(ref _stopping) != 0 || Volatile.Read(ref _faulted) != 0 ||
            Interlocked.Exchange(ref _sampling, 1) != 0)
        {
            return;
        }

        try
        {
            lock (_ioLock)
            {
                if (Volatile.Read(ref _stopping) == 0 && Volatile.Read(ref _faulted) == 0)
                {
                    CaptureAndWriteSample();
                }
            }
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _faulted, 1);
            try { _timer?.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
            SafeDisposeProcess();
            SafeTraceWarning("Diagnostics sampling stopped after an error. DeskLock will continue running.", ex);
        }
        finally
        {
            Volatile.Write(ref _sampling, 0);
        }
    }

    private void CaptureAndWriteSample()
    {
        var process = _process ?? throw new InvalidOperationException("The diagnostics process handle is unavailable.");
        var writer = _csvWriter ?? throw new InvalidOperationException("The diagnostics report is unavailable.");

        process.Refresh();
        var timestampUtc = DateTimeOffset.UtcNow;
        var cpuTime = process.TotalProcessorTime;
        var stopwatchTimestamp = Stopwatch.GetTimestamp();
        var workingSetBytes = process.WorkingSet64;
        var managedHeapBytes = GC.GetTotalMemory(false);
        var gen0Count = GC.CollectionCount(0);
        var gen1Count = GC.CollectionCount(1);
        var gen2Count = GC.CollectionCount(2);

        var elapsedSeconds = (stopwatchTimestamp - _previousTimestamp) / (double)Stopwatch.Frequency;
        var cpuSeconds = Math.Max(0, (cpuTime - _previousCpuTime).TotalSeconds);
        var cpuPercent = elapsedSeconds > 0
            ? cpuSeconds * 100.0 / (elapsedSeconds * _logicalProcessorCount)
            : 0;

        var gen0Delta = Math.Max(0, gen0Count - _previousGen0Count);
        var gen1Delta = Math.Max(0, gen1Count - _previousGen1Count);
        var gen2Delta = Math.Max(0, gen2Count - _previousGen2Count);
        var active = Interlocked.Exchange(ref _activitySincePreviousSample, 0) != 0;
        var state = active ? "Active" : "Idle";

        var line = _csvLineBuilder;
        line.Clear();
        line.Append(timestampUtc.ToString("O", CultureInfo.InvariantCulture)).Append(',')
            .Append(state).Append(',')
            .Append(workingSetBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(managedHeapBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(gen0Count.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(gen1Count.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(gen2Count.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(gen0Delta.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(gen1Delta.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(gen2Delta.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(cpuPercent.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteLine(line.ToString());

        var summary = active ? _activeSummary : _idleSummary;
        summary.Add(workingSetBytes, managedHeapBytes, cpuPercent, gen0Delta, gen1Delta, gen2Delta);

        _previousTimestamp = stopwatchTimestamp;
        _previousCpuTime = cpuTime;
        _previousGen0Count = gen0Count;
        _previousGen1Count = gen1Count;
        _previousGen2Count = gen2Count;
    }

    private void FinalizeResources()
    {
        lock (_ioLock)
        {
            if (Interlocked.Exchange(ref _finalized, 1) != 0)
            {
                return;
            }

            if (Volatile.Read(ref _started) == 0)
            {
                SafeDisposeWriter();
                SafeDisposeProcess();
                TryDeletePartialCsv();
                return;
            }

            try { _csvWriter?.Flush(); }
            catch (Exception ex) { SafeTraceWarning("Could not flush the diagnostics CSV.", ex); }

            try { WriteSummary(); }
            catch (Exception ex) { SafeTraceWarning("Could not write the diagnostics summary.", ex); }

            SafeDisposeWriter();
            SafeDisposeProcess();
        }
    }

    private void WriteSummary()
    {
        var path = _summaryPath ?? throw new InvalidOperationException("The diagnostics summary path is unavailable.");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("DeskLock runtime diagnostics summary");
        writer.WriteLine($"Session started (UTC): {_sessionStartUtc.ToString("O", CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Sampling interval (seconds): {_intervalSeconds.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine("CPU percent is normalized to total logical processor capacity.");
        writer.WriteLine("Sample state reflects DeskLock input during the preceding interval.");
        writer.WriteLine();
        WriteStateSummary(writer, "Idle", _idleSummary);
        writer.WriteLine();
        WriteStateSummary(writer, "Active", _activeSummary);
    }

    private static void WriteStateSummary(StreamWriter writer, string state, StateSummary summary)
    {
        writer.WriteLine($"{state} samples: {summary.SampleCount.ToString(CultureInfo.InvariantCulture)}");
        if (summary.SampleCount == 0)
        {
            return;
        }

        writer.WriteLine($"Average Working Set (bytes): {summary.AverageWorkingSetBytes.ToString("F0", CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Average managed heap (bytes): {summary.AverageManagedHeapBytes.ToString("F0", CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Average CPU (%): {summary.AverageCpuPercent.ToString("F2", CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Gen 0 collections during state intervals: {summary.Gen0Collections.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Gen 1 collections during state intervals: {summary.Gen1Collections.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Gen 2 collections during state intervals: {summary.Gen2Collections.ToString(CultureInfo.InvariantCulture)}");
    }

    private void SafeDisposeWriter()
    {
        try { _csvWriter?.Dispose(); }
        catch (Exception ex) { SafeTraceWarning("Could not dispose the diagnostics CSV writer.", ex); }
        finally { _csvWriter = null; }
    }

    private void SafeDisposeProcess()
    {
        try { _process?.Dispose(); }
        catch (Exception ex) { SafeTraceWarning("Could not dispose the diagnostics process handle.", ex); }
        finally { _process = null; }
    }

    private void TryDeletePartialCsv()
    {
        try
        {
            if (_csvPath is not null && File.Exists(_csvPath))
            {
                File.Delete(_csvPath);
            }
        }
        catch (Exception ex)
        {
            SafeTraceWarning("Could not remove a partial diagnostics report.", ex);
        }
    }

    private static void SafeTraceWarning(string message, Exception? exception = null)
    {
        try
        {
            if (exception is null)
            {
                Trace.TraceWarning("Memory diagnostics: {0}", message);
            }
            else
            {
                Trace.TraceWarning("Memory diagnostics: {0} {1}", message, exception.Message);
            }
        }
        catch { }
    }

    private sealed class StateSummary
    {
        private long _workingSetTotal;
        private long _managedHeapTotal;
        private double _cpuPercentTotal;

        public int SampleCount { get; private set; }
        public long Gen0Collections { get; private set; }
        public long Gen1Collections { get; private set; }
        public long Gen2Collections { get; private set; }

        public double AverageWorkingSetBytes => SampleCount == 0 ? 0 : (double)_workingSetTotal / SampleCount;
        public double AverageManagedHeapBytes => SampleCount == 0 ? 0 : (double)_managedHeapTotal / SampleCount;
        public double AverageCpuPercent => SampleCount == 0 ? 0 : _cpuPercentTotal / SampleCount;

        public void Add(long workingSetBytes, long managedHeapBytes, double cpuPercent, int gen0Delta, int gen1Delta, int gen2Delta)
        {
            SampleCount++;
            _workingSetTotal += workingSetBytes;
            _managedHeapTotal += managedHeapBytes;
            _cpuPercentTotal += cpuPercent;
            Gen0Collections += gen0Delta;
            Gen1Collections += gen1Delta;
            Gen2Collections += gen2Delta;
        }
    }
}
