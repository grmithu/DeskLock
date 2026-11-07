using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using DeskLock.Models;
using DeskLock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeskLock.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LockScreenService _lockScreen;
    private bool _suppress;
    private bool _timeTouched;

    public MainViewModel(LockScreenService lockScreen)
    {
        _lockScreen = lockScreen;

        Statuses = new ObservableCollection<StatusOption>(
            AppSettings.Statuses.Select(s => new StatusOption(s)));
        TimeOptions = new ObservableCollection<TimeOption>(
            AppSettings.TimeOptions.Select(t => new TimeOption(t.Label, t.Minutes)));

        Reset();
    }

    public ObservableCollection<StatusOption> Statuses { get; }
    public ObservableCollection<TimeOption> TimeOptions { get; }

    [ObservableProperty] private StatusOption? selectedStatus;
    [ObservableProperty] private TimeOption? selectedTime;
    [ObservableProperty] private string customMessage = "";
    [ObservableProperty] private string errorText = "";

    public string ReturnTimePreview => SelectedTime is { Minutes: > 0 } time
        ? $"Back at {DateTime.Now.AddMinutes(time.Minutes).ToString("h:mm tt", CultureInfo.InvariantCulture)}"
        : "No ETA";

    /// <summary>Popup bondho korar jonno window ke bola.</summary>
    public event Action? CloseRequested;

    partial void OnSelectedStatusChanged(StatusOption? oldValue, StatusOption? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
        if (newValue is not null && !string.IsNullOrEmpty(CustomMessage))
        {
            Suppressed(() => CustomMessage = "");
        }
    }

    partial void OnSelectedTimeChanged(TimeOption? oldValue, TimeOption? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
        OnPropertyChanged(nameof(ReturnTimePreview));
    }

    partial void OnCustomMessageChanged(string value)
    {
        if (_suppress || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        ErrorText = "";
        if (SelectedStatus is null)
        {
            return;
        }

        SelectedStatus = null;
        SelectedTime = TimeOptions.FirstOrDefault(t => t.Minutes == AppSettings.CustomMessageTheme.DefaultMinutes)
            ?? TimeOptions[0];
        _timeTouched = false;
    }

    /// <summary>Popup khulle ager state muche default status select kore.</summary>
    public void Reset()
    {
        _timeTouched = false;
        Suppressed(() => CustomMessage = "");
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

        OnPropertyChanged(nameof(ReturnTimePreview));
    }

    [RelayCommand]
    private void SelectStatus(StatusOption? option)
    {
        if (option is null) return;
        SelectedStatus = option;

        // Nijer hate time na bachle status er default time auto select hobe
        if (!_timeTouched)
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
        ErrorText = "";
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

        DateTime? returnTime = null;
        if (SelectedTime is { Minutes: > 0 } time)
        {
            returnTime = DateTime.Now.AddMinutes(time.Minutes);
        }

        var theme = SelectedStatus?.Definition ?? AppSettings.CustomMessageTheme;

        try
        {
            _lockScreen.Apply(new AwayInfo(message, returnTime, theme));
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
