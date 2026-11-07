using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DeskLock.Models;

namespace DeskLock.Services;

/// <summary>Image toiri + registry set + workstation lock.</summary>
public sealed class LockScreenService
{
    private readonly LockScreenImageRenderer _renderer = new();

    public void Apply(AwayInfo info)
    {
        string imagePath;
        try
        {
            imagePath = _renderer.Render(info);
        }
        catch (Exception ex)
        {
            LogFailure("Rendering the lock-screen image failed.", ex);
            return;
        }

        try
        {
            LockScreenRegistry.Set(imagePath);
        }
        catch (Exception ex)
        {
            LogFailure("Applying the lock-screen image failed.", ex);
        }
    }

    public void Lock()
    {
        if (!NativeMethods.LockWorkStation())
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static void LogFailure(string message, Exception exception)
    {
        var entry = $"{DateTime.Now:O} {message}{Environment.NewLine}{exception}{Environment.NewLine}";
        try
        {
            Directory.CreateDirectory(AppSettings.ImageFolder);
            File.AppendAllText(Path.Combine(AppSettings.ImageFolder, "errors.log"), entry);
        }
        catch (Exception logException)
        {
            Trace.TraceError("{0}{1}Logging the lock-screen error also failed: {2}", message, Environment.NewLine, logException);
            Trace.TraceError("{0}", entry);
        }
    }
}
