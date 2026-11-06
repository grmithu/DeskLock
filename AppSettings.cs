using System;
using System.IO;

namespace DeskLock;

/// <summary>Shob setting ekhane. Nijer moto change korun.</summary>
internal static class AppSettings
{
    // Popup khulle ei status ager theke select thakbe ("" dile kichu select thakbe na)
    public const string DefaultStatus = "Break";

    // Lock screen er upore choto caption (disabled)
    public const string CaptionText = "";
    public static readonly bool ShowDot = false;

    // Nicher dike jaruri jogajog er line (disabled)
    public const string ContactText = "";

    // Lock screen er background (na pele solid nil rong)
    public const string BackgroundImage = @"C:\Windows\Web\Screen\img100.jpg";

    public static readonly string ImageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DeskLock");

    // Name, accent RGB, default minutes, description, icon glyph, and card background RGB
    public static readonly (
        string Name,
        byte R,
        byte G,
        byte B,
        int Minutes,
        string Description,
        string IconGlyph,
        byte BackgroundR,
        byte BackgroundG,
        byte BackgroundB)[] Statuses =
    {
        ("Break",              255, 193,   7, 10, "Short break from work", "\uE769", 255, 247, 221),
        ("Lunch",              255, 152,   0, 30, "Having lunch", "\uE7C1", 255, 240, 215),
        ("Prayer",              46, 204, 113, 10, "Going for prayer", "\uE708", 229, 245, 234),
        ("Meeting",            237,  66,  69, 60, "Attending a meeting", "\uE716", 252, 229, 231),
        ("Away from Desk",     100, 181, 246,  0, "Away from my desk", "\uE7F4", 226, 240, 255),
        ("On Another Floor",     0, 150, 136,  0, "Visiting another floor", "\uE8D4", 221, 245, 242),
        ("Refreshment Break",  139, 195,  74,  0, "Tea, coffee or snacks", "\uE769", 232, 247, 223),
        ("Outside Office",     186, 104, 200, 60, "Outside the office", "\uE707", 243, 228, 248),
    };

    public static readonly (string Label, int Minutes)[] TimeOptions =
    {
        ("No ETA", 0),
        ("10 min",  10),
        ("15 min",  15),
        ("30 min",  30),
        ("1 hour",  60),
    };
}
