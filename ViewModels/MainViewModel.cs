using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using AwayLock.Models;
using AwayLock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AwayLock.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LockScreenService _lockScreen;
    private bool _suppress;
    private bool _timeTouched;

    public MainViewModel(LockScreenService lockScreen)
    {
        _lockScreen = lockScreen;

        Statuses = new ObservableCollection<StatusOption>(
            AppSettings.Statuses.Select(s => new StatusOption(s.Name, s.R, s.G, s.B, s.Minutes)));
        TimeOptions = new ObservableCollection<TimeOption>(
            AppSettings.TimeOptions.Select(t => new TimeOption(t.Label, t.Minutes)));

        Reset();
    }

    public ObservableCollection<StatusOption> Statuses { get; }
    public ObservableCollection<TimeOption> TimeOptions { get; }

    [ObservableProperty] private StatusOption? selectedStatus;
    [ObservableProperty] private TimeOption? selectedTime;
    [ObservableProperty] private string customMessage = "";
    [ObservableProperty] private string exactTime = "";
    [ObservableProperty] private string errorText = "";

    /// <summary>Popup bondho korar jonno window ke bola.</summary>
    public event Action? CloseRequested;

    partial void OnSelectedStatusChanged(StatusOption? oldValue, StatusOption? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    partial void OnSelectedTimeChanged(TimeOption? oldValue, TimeOption? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    partial void OnCustomMessageChanged(string value)
    {
        if (_suppress) return;
        if (!string.IsNullOrWhiteSpace(value)) SelectedStatus = null;
    }

    partial void OnExactTimeChanged(string value)
    {
        if (_suppress) return;
        if (!string.IsNullOrWhiteSpace(value))
        {
            SelectedTime = null;
            _timeTouched = true;
        }
    }

    /// <summary>Popup khulle ager state muche default status select kore.</summary>
    public void Reset()
    {
        _timeTouched = false;
        Suppressed(() =>
        {
            CustomMessage = "";
            ExactTime = "";
        });
        ErrorText = "";

        var def = Statuses.FirstOrDefault(s => s.Name == AppSettings.DefaultStatus);
        if (def is not null)
        {
            SelectStatus(def);
        }
        else
        {
            SelectedStatus = null;
            SelectedTime = TimeOptions[0];
        }
    }

    [RelayCommand]
    private void SelectStatus(StatusOption? option)
    {
        if (option is null) return;
        SelectedStatus = option;
        Suppressed(() => CustomMessage = "");

        // Nijer hate time na bachle status er default time auto select hobe
        if (!_timeTouched && string.IsNullOrWhiteSpace(ExactTime))
        {
            SelectedTime = TimeOptions.FirstOrDefault(t => t.Minutes == option.DefaultMinutes) ?? TimeOptions[0];
        }
        ErrorText = "";
    }

    [RelayCommand]
    private void SelectTime(TimeOption? option)
    {
        if (option is null) return;
        SelectedTime = option;
        _timeTouched = true;
        Suppressed(() => ExactTime = "");
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    [RelayCommand]
    private void Lock()
    {
        var custom = CustomMessage.Trim();
        var message = custom.Length > 0 ? custom : SelectedStatus?.Name;
        if (message is null)
        {
            ErrorText = "Choose a status or type a message.";
            return;
        }

        var subText = "";
        var exact = ExactTime.Trim();
        if (exact.Length > 0)
        {
            subText = $"Back at {exact}";
        }
        else if (SelectedTime is { Minutes: > 0 } time)
        {
            var back = DateTime.Now.AddMinutes(time.Minutes);
            subText = "Back at " + back.ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        var accent = custom.Length > 0 || SelectedStatus is null ? Colors.White : SelectedStatus.AccentColor;

        try
        {
            _lockScreen.Apply(new AwayInfo(message, subText, accent));
        }
        catch (Exception ex)
        {
            ErrorText = "Could not set the lock screen: " + ex.Message;
            return;
        }

        CloseRequested?.Invoke();

        try
        {
            _lockScreen.Lock();
        }
        catch (Exception ex)
        {
            ErrorText = "Could not lock the PC: " + ex.Message;
        }
    }

    private void Suppressed(Action action)
    {
        _suppress = true;
        try { action(); }
        finally { _suppress = false; }
    }
}
