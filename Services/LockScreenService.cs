using System.ComponentModel;
using System.Runtime.InteropServices;
using AwayLock.Models;

namespace AwayLock.Services;

/// <summary>Image toiri + registry set + workstation lock.</summary>
public sealed class LockScreenService
{
    private readonly LockScreenImageRenderer _renderer = new();

    public void Apply(AwayInfo info)
    {
        var imagePath = _renderer.Render(info);
        LockScreenRegistry.Set(imagePath);
    }

    public void Lock()
    {
        if (!NativeMethods.LockWorkStation())
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
