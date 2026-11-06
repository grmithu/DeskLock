# Away Lock (WPF, .NET 10, MVVM)

Hotkey (`Ctrl+Alt+L`) chaple popup ashe. Status + fire asar somoy bachle laptop lock hoy
ar lock screen e Windows er background er upor editorial style e message dekhay.
Anlock korle message nije theke muche jay.

## Requirements
- Windows 11 **Pro / Enterprise / Education** (Home e lock screen setting kaj kore na)
- Admin access (app `requireAdministrator` manifest diye chole)
- Build korte: .NET 10 SDK (https://dotnet.microsoft.com/download)

## Build & run
Admin Terminal (PowerShell/CMD "Run as administrator") theke, project folder e:

    dotnet build
    dotnet run

> Non-admin terminal theke `dotnet run` dile "requires elevation" error ashe — eta shabhabik.
> Visual Studio use korle VS ke "Run as administrator" diye khulun.

## Ekta .exe banano (share korar jonno)

Shoto: chhoto size, target PC te .NET 10 Desktop Runtime lagbe:

    dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

Runtime chhara chalate chaile (boro size):

    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

Output: `bin\Release\net10.0-windows\win-x64\publish\AwayLock.exe`

## Prothom bar
1. `AwayLock.exe` chalan (UAC te Yes) — tray te nil gol icon ashbe.
2. Tray icon e right-click > **Start with Windows** tick din (login e auto chalu hobe, UAC chhara).
3. `Ctrl+Alt+L` chapun > status bachun > **Enter**.

## Setting
`AppSettings.cs` e: default status, status/rong/default somoy, caption (`AWAY`), jaruri jogajog line,
background image. Change korle abar build korte hobe.

## Bondho / uninstall
Tray > Exit. Tray > Start with Windows untick. Folder delete. (Registry value app nije muche dey.)

## Structure
- `Models/`      StatusOption, TimeOption, AwayInfo
- `ViewModels/`  MainViewModel (CommunityToolkit.Mvvm)
- `Views/`       MainWindow (popup)
- `Services/`    LockScreenImageRenderer, LockScreenRegistry, LockScreenService,
                 HotkeyService, SessionWatcher, StartupService, TrayService

## Share korar age
- Unsigned .exe te Windows SmartScreen warning dekhate pare.
- App registry (HKLM) e likhe — office laptop e deowar age IT ke jaanan.
