using System;
using System.IO;
using DeskLock.Models;

namespace DeskLock;

/// <summary>Shob setting ekhane. Nijer moto change korun.</summary>
internal static class AppSettings
{
    // Popup khulle ei status ager theke select thakbe ("" dile kichu select thakbe na)
    public const string DefaultStatus = "Break";

    public static readonly string ImageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskLock");

    public static readonly StatusDefinition CustomMessageTheme =
        new("Custom Message", 0, "Custom away message", "#A02D6B", "#F2CFE0", "message-dots");

    public static readonly StatusDefinition[] Statuses =
    {
        new("Break", 10, "Short break from work", "#854F0B", "#FAC775", "coffee"),
        new("Lunch", 30, "Having lunch", "#993C1D", "#F5C4B3", "tools-kitchen-2"),
        new("Prayer", 10, "Going for prayer", "#1E5A34", "#C8EBD5", "building-mosque"),
        new("Meeting", 60, "Attending a meeting", "#A32D2D", "#F7C1C1", "users"),
        new("Away from Desk", 0, "Away from my desk", "#185FA5", "#B5D4F4", "walk"),
        new("On Another Floor", 0, "Visiting another floor", "#0F6E56", "#9FE1CB", "stairs"),
        new("Refreshment Break", 0, "Tea, coffee or snacks", "#4A5F14", "#D6E6A0", "cookie"),
        new("Outside Office", 60, "Outside the office", "#534AB7", "#CECBF6", "map-pin"),
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
