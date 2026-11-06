using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskLock.Models;

public partial class StatusOption : ObservableObject
{
    public StatusOption(
        string name,
        byte r,
        byte g,
        byte b,
        int defaultMinutes,
        string description,
        string iconGlyph,
        byte backgroundR,
        byte backgroundG,
        byte backgroundB)
    {
        Name = name;
        AccentColor = Color.FromRgb(r, g, b);
        Brush = new SolidColorBrush(AccentColor);
        Brush.Freeze();
        DefaultMinutes = defaultMinutes;
        Description = description;
        IconGlyph = iconGlyph;
        CardBackground = new SolidColorBrush(Color.FromRgb(backgroundR, backgroundG, backgroundB));
        CardBackground.Freeze();
    }

    public string Name { get; }
    public Color AccentColor { get; }
    public SolidColorBrush Brush { get; }
    public int DefaultMinutes { get; }
    public string Description { get; }
    public string IconGlyph { get; }
    public SolidColorBrush CardBackground { get; }

    [ObservableProperty]
    private bool isSelected;
}
