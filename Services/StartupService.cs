using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;

namespace DeskLock.Services;

/// <summary>
/// Windows login e app auto chalu korar jonno Task Scheduler task (admin shoho, UAC chhara).
/// XML use kora hoy, jate laptop battery te thakleo cholte pare ar 3 din por bondho na hoy.
/// </summary>
public static class StartupService
{
    private const string TaskName = "DeskLockApp";
    private static readonly string SetupCompletePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DeskLock",
        "startup-configured");

    public static bool IsEnabled() => RunSchtasks($"/query /tn \"{TaskName}\"", out _) == 0;

    public static bool ConfigureOnFirstRun(out string? error)
    {
        error = null;
        if (File.Exists(SetupCompletePath)) return true;
        if (!Enable(out error)) return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SetupCompletePath)!);
            File.WriteAllText(SetupCompletePath, string.Empty);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            error = $"Startup was enabled, but DeskLock could not save its setup state: {ex.Message}";
            return false;
        }
    }

    public static bool Enable(out string? error)
    {
        error = null;
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            error = "Could not determine the DeskLock executable path.";
            return false;
        }

        var user = SecurityElement.Escape($"{Environment.UserDomainName}\\{Environment.UserName}");
        var command = SecurityElement.Escape(exe);

        var xml = $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                  <UserId>{user}</UserId>
                </LogonTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{user}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>HighestAvailable</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <AllowHardTerminate>true</AllowHardTerminate>
                <StartWhenAvailable>true</StartWhenAvailable>
                <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                <Enabled>true</Enabled>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{command}</Command>
                </Exec>
              </Actions>
            </Task>
            """;

        var xmlPath = Path.Combine(Path.GetTempPath(), $"DeskLockTask-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(xmlPath, xml, Encoding.Unicode);
            return RunSchtasks($"/create /tn \"{TaskName}\" /xml \"{xmlPath}\" /f", out error) == 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            try { File.Delete(xmlPath); } catch { /* ignore */ }
        }
    }

    public static bool Disable(out string? error) =>
        RunSchtasks($"/delete /tn \"{TaskName}\" /f", out error) == 0;

    private static int RunSchtasks(string arguments, out string? error)
    {
        error = null;
        var info = new ProcessStartInfo("schtasks.exe", arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using var process = Process.Start(info);
            if (process is null)
            {
                error = "Windows did not start schtasks.exe.";
                return -1;
            }

            var output = process.StandardOutput.ReadToEnd();
            var errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                error = !string.IsNullOrWhiteSpace(errorOutput)
                    ? errorOutput.Trim()
                    : !string.IsNullOrWhiteSpace(output)
                        ? output.Trim()
                        : $"schtasks.exe exited with code {process.ExitCode}.";
            }

            return process.ExitCode;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            error = ex.Message;
            return -1;
        }
    }
}
