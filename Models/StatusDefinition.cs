using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DeskLock.Models;

public sealed class StatusDefinition
{
    public StatusDefinition(
        string name,
        int defaultMinutes,
        string description,
        string lockColor,
        string lockSubColor,
        string iconKey)
    {
        Name = name;
        DefaultMinutes = defaultMinutes;
        Description = description;
        LockColor = (Color)ColorConverter.ConvertFromString(lockColor);
        LockSubColor = (Color)ColorConverter.ConvertFromString(lockSubColor);
        IconKey = iconKey;
    }

    public string Name { get; }
    public int DefaultMinutes { get; }
    public string Description { get; }
    public Color LockColor { get; }
    public Color LockSubColor { get; }
    public string IconKey { get; }
    public Geometry IconGeometry => IconGeometries[IconKey];

    private static readonly IReadOnlyDictionary<string, Geometry> IconGeometries =
        new Dictionary<string, Geometry>(StringComparer.Ordinal)
        {
            ["coffee"] = Parse("M3 14c.83 .642 2.077 1.017 3.5 1c1.423 .017 2.67 -.358 3.5 -1c.83 -.642 2.077 -1.017 3.5 -1c1.423 -.017 2.67 .358 3.5 1 M8 3a2.4 2.4 0 0 0 -1 2a2.4 2.4 0 0 0 1 2 M12 3a2.4 2.4 0 0 0 -1 2a2.4 2.4 0 0 0 1 2 M3 10h14v5a6 6 0 0 1 -6 6h-2a6 6 0 0 1 -6 -6v-5 M16.746 16.726a3 3 0 1 0 .252 -5.555"),
            ["tools-kitchen-2"] = Parse("M19 3v12h-5c-.023 -3.681 .184 -7.406 5 -12m0 12v6h-1v-3m-10 -14v17m-3 -17v3a3 3 0 1 0 6 0v-3"),
            ["building-mosque"] = Parse("M3 21h7v-2a2 2 0 1 1 4 0v2h7 M4 21v-10 M20 21v-10 M4 16h3v-3h10v3h3 M17 13a5 5 0 0 0 -10 0 M21 10.5c0 -.329 -.077 -.653 -.224 -.947l-.776 -1.553l-.776 1.553a2.118 2.118 0 0 0 -.224 .947a.5 .5 0 0 0 .5 .5h1a.5 .5 0 0 0 .5 -.5 M5 10.5c0 -.329 -.077 -.653 -.224 -.947l-.776 -1.553l-.776 1.553a2.118 2.118 0 0 0 -.224 .947a.5 .5 0 0 0 .5 .5h1a.5 .5 0 0 0 .5 -.5 M12 2a2 2 0 1 0 2 2 M12 6v2"),
            ["users"] = Parse("M5 7a4 4 0 1 0 8 0a4 4 0 1 0 -8 0 M3 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2 M16 3.13a4 4 0 0 1 0 7.75 M21 21v-2a4 4 0 0 0 -3 -3.85"),
            ["walk"] = Parse("M12 4a1 1 0 1 0 2 0a1 1 0 1 0 -2 0 M7 21l3 -4 M16 21l-2 -4l-3 -3l1 -6 M6 12l2 -3l4 -1l3 3l3 1"),
            ["stairs"] = Parse("M22 5h-5v5h-5v5h-5v5h-5"),
            ["cookie"] = Parse("M8 13v.01 M12 17v.01 M12 12v.01 M16 14v.01 M11 8v.01 M13.148 3.476l2.667 1.104a4 4 0 0 0 4.656 6.14l.053 .132a3 3 0 0 1 0 2.296q -.745 1.18 -1.024 1.852q -.283 .684 -.66 2.216a3 3 0 0 1 -1.624 1.623q -1.572 .394 -2.216 .661q -.712 .295 -1.852 1.024a3 3 0 0 1 -2.296 0q -1.203 -.754 -1.852 -1.024q -.707 -.292 -2.216 -.66a3 3 0 0 1 -1.623 -1.624q -.397 -1.577 -.661 -2.216q -.298 -.718 -1.024 -1.852a3 3 0 0 1 0 -2.296q .719 -1.116 1.024 -1.852q .257 -.62 .66 -2.216a3 3 0 0 1 1.624 -1.623q 1.547 -.384 2.216 -.661q .687 -.285 1.852 -1.024a3 3 0 0 1 2.296 0"),
            ["map-pin"] = Parse("M9 11a3 3 0 1 0 6 0a3 3 0 1 0 -6 0 M17.657 16.657l-4.243 4.243a2 2 0 0 1 -2.827 0l-4.244 -4.243a8 8 0 1 1 11.314 0"),
        };

    private static Geometry Parse(string path)
    {
        var geometry = Geometry.Parse(path);
        geometry.Freeze();
        return geometry;
    }
}
