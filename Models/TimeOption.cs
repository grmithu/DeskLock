using CommunityToolkit.Mvvm.ComponentModel;

namespace AwayLock.Models;

public partial class TimeOption : ObservableObject
{
    public TimeOption(string label, int minutes)
    {
        Label = label;
        Minutes = minutes;
    }

    public string Label { get; }
    public int Minutes { get; }

    [ObservableProperty]
    private bool isSelected;
}
