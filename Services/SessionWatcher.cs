using System;
using Microsoft.Win32;

namespace AwayLock.Services;

/// <summary>Anlock korle lock screen er message muche dey (ager moto auto reset).</summary>
public sealed class SessionWatcher : IDisposable
{
    public SessionWatcher()
    {
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    private static void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon)
        {
            try { LockScreenRegistry.Clear(); } catch { /* ignore */ }
        }
    }

    public void Dispose()
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }
}
