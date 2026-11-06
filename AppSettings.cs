using System;
using System.IO;

namespace DeskLock;

/// <summary>Shob setting ekhane. Nijer moto change korun.</summary>
internal static class AppSettings
{
    // Popup khulle ei status ager theke select thakbe ("" dile kichu select thakbe na)
    public const string DefaultStatus = "Break";

    // Lock screen er upore choto caption
    public const string CaptionText = "AWAY";
    public const bool ShowDot = true;

    // Nicher dike jaruri jogajog er line ("" dile dekhabe na)
    public const string ContactText = "Urgent? Please call me";

    // Lock screen er background (na pele solid nil rong)
    public const string BackgroundImage = @"C:\Windows\Web\Screen\img100.jpg";

    public static readonly string ImageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DeskLock");

    // Name, R, G, B, default fire asar somoy (minutes)
    public static readonly (string Name, byte R, byte G, byte B, int Minutes)[] Statuses =
    {
        ("Break",          255, 193,   7, 15),
        ("Prayer",          46, 204, 113, 15),
        ("Lunch",          255, 152,   0, 60),
        ("Meeting",        237,  66,  69, 60),
        ("Away from Desk", 100, 181, 246, 30),
        ("Outside Office", 186, 104, 200,  0),
    };

    public static readonly (string Label, int Minutes)[] TimeOptions =
    {
        ("No time", 0),
        ("15 min",  15),
        ("30 min",  30),
        ("1 hour",  60),
        ("2 hours", 120),
    };
}
