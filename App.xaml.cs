using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using DeskLock.Services;
using DeskLock.ViewModels;
using DeskLock.Views;

namespace DeskLock;

public partial class App : Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _waitHandle;
    private TrayService? _tray;
    private HotkeyService? _hotkey;
    private SessionWatcher? _session;
    private MemoryDiagnosticsService? _memoryDiagnostics;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (Array.Exists(e.Args, arg => string.Equals(arg, "--render-themes", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var paths = new LockScreenImageRenderer().RenderThemePreviews();
                Trace.TraceInformation("Generated lock-screen previews:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, paths));
                Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Could not render previews", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }

            return;
        }

        // Ek shomoye shudhu ekta instance. Dwitiyo bar chalale ager instance er popup khule dey.
        _mutex = new Mutex(true, "DeskLock.SingleInstance", out var isFirst);
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DeskLock.ShowPopup");
        if (!isFirst)
        {
            _showEvent.Set();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Desk Lock", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Ager bar er kono message lock screen e theke gele muche dao
        try { LockScreenRegistry.Clear(); } catch { /* ignore */ }

        var viewModel = new MainViewModel(new LockScreenService());
        _window = new MainWindow { DataContext = viewModel };
        viewModel.CloseRequested += () => _window.Hide();

        if (Array.Exists(e.Args, arg => string.Equals(arg, "--memory-diagnostics", StringComparison.OrdinalIgnoreCase)))
        {
            var intervalSeconds = GetMemoryDiagnosticsInterval(e.Args);
            if (MemoryDiagnosticsService.TryStart(intervalSeconds, out var diagnostics) && diagnostics is not null)
            {
                _memoryDiagnostics = diagnostics;
                _window.ActivityOccurred += diagnostics.MarkActivity;
            }
        }

        var startupConfigured = StartupService.ConfigureOnFirstRun(out var startupError);
        _tray = new TrayService();
        _tray.OpenRequested += ShowPopup;
        _tray.ExitRequested += ExitApp;
        if (_memoryDiagnostics is not null)
        {
            _tray.ActivityOccurred += _memoryDiagnostics.MarkActivity;
        }

        if (!startupConfigured)
        {
            _tray.Notify(
                "Desk Lock",
                $"Could not configure Start with Windows. DeskLock is still running. {startupError}");
        }

        _hotkey = new HotkeyService();
        _hotkey.Pressed += ShowPopup;
        var hotkeyOk = _hotkey.Register(ModifierKeys.Control | ModifierKeys.Alt, Key.L);

        _session = new SessionWatcher();

        _waitHandle = ThreadPool.RegisterWaitForSingleObject(
            _showEvent, (_, _) => Dispatcher.BeginInvoke(new Action(ShowPopup)), null, Timeout.Infinite, false);

        _tray.Notify(
            "Desk Lock is running",
            hotkeyOk
                ? "Press Ctrl+Alt+L to set your status."
                : $"Could not register Ctrl+Alt+L: {_hotkey.RegistrationError} Use the tray icon instead.");
    }

    private void ShowPopup()
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        _memoryDiagnostics?.MarkActivity();
        Dispatcher.BeginInvoke(new Action(() => _window?.ShowPopup()));
    }

    private void ExitApp()
    {
        _memoryDiagnostics?.MarkActivity();
        if (_window is not null) _window.AllowClose = true;
        Shutdown();
    }

    private static int GetMemoryDiagnosticsInterval(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--memory-interval-seconds", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 < args.Length &&
                int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var intervalSeconds) &&
                intervalSeconds is >= MemoryDiagnosticsService.MinimumIntervalSeconds and <= MemoryDiagnosticsService.MaximumIntervalSeconds)
            {
                return intervalSeconds;
            }

            SafeTraceWarning($"Invalid --memory-interval-seconds value; using the {MemoryDiagnosticsService.DefaultIntervalSeconds}-second default.");
            return MemoryDiagnosticsService.DefaultIntervalSeconds;
        }

        return MemoryDiagnosticsService.DefaultIntervalSeconds;
    }

    private static void SafeTraceWarning(string message)
    {
        try { Trace.TraceWarning("Memory diagnostics: {0}", message); } catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_memoryDiagnostics is not null)
        {
            if (_window is not null)
            {
                _window.ActivityOccurred -= _memoryDiagnostics.MarkActivity;
            }
            if (_tray is not null)
            {
                _tray.ActivityOccurred -= _memoryDiagnostics.MarkActivity;
            }
            try { _memoryDiagnostics.Dispose(); } catch { }
            _memoryDiagnostics = null;
        }

        // Shudhu prothom instance message muche dey (dwitiyo instance na)
        if (_window is not null)
        {
            try { LockScreenRegistry.Clear(); } catch { /* ignore */ }
        }

        _waitHandle?.Unregister(null);
        _hotkey?.Dispose();
        _session?.Dispose();
        _tray?.Dispose();
        _showEvent?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
