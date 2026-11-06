using System;
using System.Windows.Input;
using System.Windows.Interop;

namespace DeskLock.Services;

/// <summary>Global hotkey (jemon Ctrl+Alt+L). Message-only window diye kaj kore.</summary>
public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0xA11C;
    private const uint ModNoRepeat = 0x4000;
    private static readonly IntPtr HwndMessage = new(-3);

    private HwndSource? _source;

    public event Action? Pressed;

    public bool Register(ModifierKeys modifiers, Key key)
    {
        // WPF ModifierKeys er man (Alt=1, Ctrl=2, Shift=4, Win=8) Win32 er shathe milay
        _source = new HwndSource(0, 0, 0, 0, 0, "DeskLockHotkeyWindow", HwndMessage);
        _source.AddHook(WndProc);
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return NativeMethods.RegisterHotKey(_source.Handle, HotkeyId, (uint)modifiers | ModNoRepeat, vk);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is null) return;
        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _source = null;
    }
}
