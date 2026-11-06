using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskLock.Models;

public partial class StatusOption : ObservableObject
{
    public StatusOption(string name, byte r, byte g, byte b, int defaultMinutes)
    {
        Name = name;
        AccentColor = Color.FromRgb(r, g, b);
        Brush = new SolidColorBrush(AccentColor);
        Brush.Freeze();
        DefaultMinutes = defaultMinutes;
    }

    public string Name { get; }
    public Color AccentColor { get; }
    public SolidColorBrush Brush { get; }
    public int DefaultMinutes { get; }

    [ObservableProperty]
    private bool isSelected;
}
