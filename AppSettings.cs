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

    // Name, R, G, B, default fire asar somoy (minutes)
    public static readonly (string Name, byte R, byte G, byte B, int Minutes)[] Statuses =
    {
        ("Break",          255, 193,   7, 10),
        ("Lunch",          255, 152,   0, 30),
        ("Prayer",          46, 204, 113, 10),
        ("Meeting",        237,  66,  69, 60),
        ("Away from Desk", 100, 181, 246,  0),
        ("On Another Floor",  0, 150, 136,  0),
        ("Refreshment Break", 139, 195,  74,  0),
        ("Outside Office", 186, 104, 200, 60),
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
