using System;
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
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        _tray = new TrayService();
        _tray.OpenRequested += ShowPopup;
        _tray.ExitRequested += ExitApp;

        _hotkey = new HotkeyService();
        _hotkey.Pressed += ShowPopup;
        var hotkeyOk = _hotkey.Register(ModifierKeys.Control | ModifierKeys.Alt, Key.L);

        _session = new SessionWatcher();

        _waitHandle = ThreadPool.RegisterWaitForSingleObject(
            _showEvent, (_, _) => Dispatcher.BeginInvoke(new Action(ShowPopup)), null, Timeout.Infinite, false);

        _tray.Notify(
            "Desk Lock is running",
            hotkeyOk ? "Press Ctrl+Alt+L to set your status." : "Ctrl+Alt+L is already used by another app. Use the tray icon instead.");
    }

    private void ShowPopup() => _window?.ShowPopup();

    private void ExitApp()
    {
        if (_window is not null) _window.AllowClose = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
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
