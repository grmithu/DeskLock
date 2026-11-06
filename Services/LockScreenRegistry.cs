using Microsoft.Win32;

namespace DeskLock.Services;

/// <summary>Windows er lock screen image registry (PersonalizationCSP) e set/clear kore. Admin lage.</summary>
public static class LockScreenRegistry
{
    private const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";
    private static readonly string[] ValueNames =
    {
        "LockScreenImagePath", "LockScreenImageUrl", "LockScreenImageStatus"
    };

    public static void Set(string imagePath)
    {
        using var key = Registry.LocalMachine.CreateSubKey(KeyPath, writable: true);
        key.SetValue("LockScreenImagePath", imagePath, RegistryValueKind.String);
        key.SetValue("LockScreenImageUrl", imagePath, RegistryValueKind.String);
        key.SetValue("LockScreenImageStatus", 1, RegistryValueKind.DWord);
    }

    public static void Clear()
    {
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true);
        if (key is null) return;
        foreach (var name in ValueNames)
        {
            key.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}
