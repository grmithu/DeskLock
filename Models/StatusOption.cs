using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskLock.Models;

public partial class StatusOption : ObservableObject
{
    public StatusOption(
        StatusDefinition definition)
    {
        Definition = definition;
        Name = definition.Name;
        AccentColor = definition.LockColor;
        Brush = new SolidColorBrush(AccentColor);
        Brush.Freeze();
        DefaultMinutes = definition.DefaultMinutes;
        Description = definition.Description;
        IconGeometry = definition.IconGeometry;
        CardBackground = new SolidColorBrush(Color.FromRgb(
            (byte)(AccentColor.R * 0.1 + 255 * 0.9),
            (byte)(AccentColor.G * 0.1 + 255 * 0.9),
            (byte)(AccentColor.B * 0.1 + 255 * 0.9)));
        CardBackground.Freeze();
    }

    public StatusDefinition Definition { get; }
    public string Name { get; }
    public Color AccentColor { get; }
    public SolidColorBrush Brush { get; }
    public int DefaultMinutes { get; }
    public string Description { get; }
    public Geometry IconGeometry { get; }
    public SolidColorBrush CardBackground { get; }

    [ObservableProperty]
    private bool isSelected;
}
