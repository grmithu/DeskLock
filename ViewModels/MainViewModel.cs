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
    [ObservableProperty] private string exactTime = "";
    [ObservableProperty] private string? exactTimeHour;
    [ObservableProperty] private string? exactTimeMinute;
    [ObservableProperty] private string? exactTimePeriod;
    [ObservableProperty] private bool isExactTimePickerOpen;
    [ObservableProperty] private bool isSelectingMinutes;
    [ObservableProperty] private double dialHandX = 82;
    [ObservableProperty] private double dialHandY = 22;
    [ObservableProperty] private string exactTimeError = "";
    [ObservableProperty] private string errorText = "";

    public bool HasExactTimeError => !string.IsNullOrEmpty(ExactTimeError);
    public string ExactTimeDisplay => string.IsNullOrEmpty(ExactTime) ? "Select time..." : ExactTime;
    public string ClockHourDisplay => ExactTimeHour ?? "12";
    public string ClockMinuteDisplay => ExactTimeMinute ?? "00";
    public string ClockPeriodDisplay => ExactTimePeriod ?? "AM";
    public string ClockPickerInstruction => IsSelectingMinutes ? "Select minute" : "Select hour";
    public string ClockDialSelectionDisplay => IsSelectingMinutes
        ? ExactTimeMinute ?? "00"
        : ExactTimeHour ?? "12";

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
    }

    partial void OnCustomMessageChanged(string value)
    {
        if (!_suppress && !string.IsNullOrEmpty(value))
        {
            SelectedStatus = null;
        }
    }

    partial void OnExactTimeErrorChanged(string value) =>
        OnPropertyChanged(nameof(HasExactTimeError));

    partial void OnExactTimeHourChanged(string? value)
    {
        OnPropertyChanged(nameof(ClockHourDisplay));
        OnPropertyChanged(nameof(ClockDialSelectionDisplay));
        UpdateClockHand();
        UpdateExactTimeFromPicker();
    }

    partial void OnExactTimeMinuteChanged(string? value)
    {
        OnPropertyChanged(nameof(ClockMinuteDisplay));
        OnPropertyChanged(nameof(ClockDialSelectionDisplay));
        UpdateClockHand();
        UpdateExactTimeFromPicker();
    }

    partial void OnExactTimePeriodChanged(string? value)
    {
        OnPropertyChanged(nameof(ClockPeriodDisplay));
        UpdateExactTimeFromPicker();
    }

    partial void OnIsSelectingMinutesChanged(bool value)
    {
        OnPropertyChanged(nameof(ClockPickerInstruction));
        OnPropertyChanged(nameof(ClockDialSelectionDisplay));
        UpdateClockHand();
    }

    partial void OnExactTimeChanged(string value)
    {
        OnPropertyChanged(nameof(ExactTimeDisplay));
        if (_suppress) return;
        if (!string.IsNullOrWhiteSpace(value))
        {
            SelectedTime = null;
            _timeTouched = true;
            ExactTimeError = ValidateExactTime(value);
        }
        else
        {
            ExactTimeError = "";
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
            ExactTimeHour = null;
            ExactTimeMinute = null;
            ExactTimePeriod = null;
            ExactTimeError = "";
        });
        IsExactTimePickerOpen = false;
        IsSelectingMinutes = false;
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
        Suppressed(() =>
        {
            ExactTime = "";
            ExactTimeHour = null;
            ExactTimeMinute = null;
            ExactTimePeriod = null;
        });
        IsExactTimePickerOpen = false;
        IsSelectingMinutes = false;
        ExactTimeError = "";
    }

    [RelayCommand]
    private void ToggleExactTimePicker()
    {
        IsExactTimePickerOpen = !IsExactTimePickerOpen;
        if (IsExactTimePickerOpen)
        {
            IsSelectingMinutes = ExactTimeHour is not null;
            ExactTimePeriod ??= "AM";
        }
    }

    [RelayCommand]
    private void CloseExactTimePicker() => IsExactTimePickerOpen = false;

    [RelayCommand]
    private void SelectHourPhase() => IsSelectingMinutes = false;

    [RelayCommand]
    private void SelectMinutePhase() => IsSelectingMinutes = true;

    [RelayCommand]
    private void SelectExactTimePeriod(string? period)
    {
        if (period is "AM" or "PM")
        {
            ExactTimePeriod = period;
        }
    }

    public void SelectClockDialPoint(double x, double y)
    {
        var dx = x - 82;
        var dy = 82 - y;
        var angle = Math.Atan2(dx, dy);
        if (angle < 0) angle += 2 * Math.PI;

        if (IsSelectingMinutes)
        {
            ExactTimeMinute = ((int)Math.Round(angle / (2 * Math.PI) * 60) % 60)
                .ToString("00", CultureInfo.InvariantCulture);
        }
        else
        {
            var hour = (int)Math.Round(angle / (2 * Math.PI) * 12) % 12;
            ExactTimeHour = (hour == 0 ? 12 : hour).ToString(CultureInfo.InvariantCulture);
            ExactTimePeriod ??= "AM";
            IsSelectingMinutes = true;
        }
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
        var lockMoment = DateTime.Now;
        var exact = ExactTime.Trim();
        if (exact.Length > 0)
        {
            ExactTimeError = ValidateExactTime(exact);
            if (HasExactTimeError) return;
            var parsedTime = DateTime.ParseExact(
                exact,
                "h:mm tt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            returnTime = lockMoment.Date.Add(parsedTime.TimeOfDay);
        }
        else if (SelectedTime is { Minutes: > 0 } time)
        {
            returnTime = lockMoment.AddMinutes(time.Minutes);
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

    private void UpdateExactTimeFromPicker()
    {
        if (_suppress
            || ExactTimeHour is null
            || ExactTimeMinute is null
            || ExactTimePeriod is null)
        {
            return;
        }

        ExactTime = $"{ExactTimeHour}:{ExactTimeMinute} {ExactTimePeriod}";
    }

    private void UpdateClockHand()
    {
        var angle = IsSelectingMinutes
            ? (ExactTimeMinute is not null
                ? int.Parse(ExactTimeMinute, CultureInfo.InvariantCulture) * 6
                : 0)
            : (ExactTimeHour is not null
                ? int.Parse(ExactTimeHour, CultureInfo.InvariantCulture) % 12 * 30
                : 0);
        var radians = angle * Math.PI / 180;
        DialHandX = 82 + 60 * Math.Sin(radians);
        DialHandY = 82 - 60 * Math.Cos(radians);
    }

    private static string ValidateExactTime(string value)
    {
        var exact = value.Trim();
        if (!DateTime.TryParseExact(
                exact,
                "h:mm tt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedTime)
            || !string.Equals(
                exact,
                parsedTime.ToString("h:mm tt", CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            return "Enter the time in the 2:00 PM format.";
        }

        var now = DateTime.Now;
        var target = now.Date.Add(parsedTime.TimeOfDay);
        return target <= now
            ? "That time has already passed. Enter a future time today."
            : "";
    }
}
