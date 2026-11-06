using System;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace DeskLock.Services;

/// <summary>System tray icon + menu (WinForms NotifyIcon, kono extra package lage na).</summary>
public sealed class TrayService : IDisposable
{
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ToolStripMenuItem _startupItem;

    public event Action? OpenRequested;
    public event Action? ExitRequested;

    public TrayService()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Set status...", null, (_, _) => OpenRequested?.Invoke());

        _startupItem = new WinForms.ToolStripMenuItem("Start with Windows") { Checked = StartupService.IsEnabled() };
        _startupItem.Click += (_, _) => ToggleStartup();
        menu.Items.Add(_startupItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());

        _icon = new WinForms.NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "Desk Lock (Ctrl+Alt+L)",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left) OpenRequested?.Invoke();
        };
    }

    public void Notify(string title, string text) =>
        _icon.ShowBalloonTip(3000, title, text, WinForms.ToolTipIcon.Info);

    private void ToggleStartup()
    {
        var ok = _startupItem.Checked
            ? StartupService.Disable(out var error)
            : StartupService.Enable(out error);
        _startupItem.Checked = StartupService.IsEnabled();
        if (!ok) Notify("Desk Lock", $"Could not change the startup setting. {error}");
    }

    private static Drawing.Icon CreateIcon()
    {
        using var bitmap = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Drawing.Color.Transparent);
            using var blue = new Drawing.SolidBrush(Drawing.Color.FromArgb(0, 120, 215));
            using var white = new Drawing.SolidBrush(Drawing.Color.White);
            g.FillEllipse(blue, 2, 2, 28, 28);
            g.FillEllipse(white, 11, 11, 10, 10);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temp = Drawing.Icon.FromHandle(handle);
            return (Drawing.Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
