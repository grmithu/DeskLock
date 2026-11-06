using System;
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

    public static bool IsEnabled() => RunSchtasks($"/query /tn \"{TaskName}\"") == 0;

    public static bool Enable()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe)) return false;

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

        var xmlPath = Path.Combine(Path.GetTempPath(), "DeskLockTask.xml");
        try
        {
            File.WriteAllText(xmlPath, xml, Encoding.Unicode);
            return RunSchtasks($"/create /tn \"{TaskName}\" /xml \"{xmlPath}\" /f") == 0;
        }
        finally
        {
            try { File.Delete(xmlPath); } catch { /* ignore */ }
        }
    }

    public static bool Disable() => RunSchtasks($"/delete /tn \"{TaskName}\" /f") == 0;

    private static int RunSchtasks(string arguments)
    {
        var info = new ProcessStartInfo("schtasks.exe", arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(info);
        if (process is null) return -1;
        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode;
    }
}
