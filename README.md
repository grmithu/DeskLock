# Desk Lock (WPF, .NET 10, MVVM)

Hotkey (`Ctrl+Alt+L`) chaple popup ashe. Status + fire asar somoy bachle laptop lock hoy
ar lock screen e status-onujayi solid-color theme, icon, message, ebong ferar somoy dekhay.
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

## Preview themes
Lock na kore shob status theme-er PNG preview toiri korte:

    dotnet run -- --render-themes

Run elevated (the app manifest requests administrator access); this command does not lock the PC.
Preview gulo `%AppData%\DeskLock\previews\` e save hobe. Lock screen image `%AppData%\DeskLock\lock.png` e save hoy.
Windows Spotlight off kore **Settings > Personalization > Lock screen > Picture** select korun,
nahole Windows app-er image ignore korte pare.

## Ekta .exe banano (share korar jonno)

Shoto: chhoto size, target PC te .NET 10 Desktop Runtime lagbe:

    dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

Runtime chhara chalate chaile (boro size):

    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

Output: `bin\Release\net10.0-windows\win-x64\publish\DeskLock.exe`

## Recommended workflow
1. Run `DeskLock.exe` (approve the UAC prompt). DeskLock configures Windows login startup on the first successful launch and stays in the system tray; the status dialog does not open automatically.
2. Press `Ctrl+Alt+L` at any time, or click the tray icon, to open the status dialog.
3. Choose a status and press **Enter** or **Lock now** to apply it and lock the PC.
4. Press **Esc** or **Cancel** to hide the dialog. DeskLock keeps running in the tray.
5. Choose **Exit** from the tray menu to close DeskLock completely.

Use the tray menu's **Start with Windows** option to disable or re-enable automatic login startup. If automatic setup fails, DeskLock remains running and shows an error notification; use **Start with Windows** in the tray menu to retry.

## Prothom bar
1. `DeskLock.exe` chalan (UAC te Yes) — tray te nil gol icon ashbe, dialog khulbe na.
2. `Ctrl+Alt+L` chapun > status bachun > **Enter**.
3. Login e auto chalu hobe; tray menu theke **Start with Windows** untick kore eta bondho korte parben.

## Setting
`AppSettings.cs` e default status, status theme color/icon, description, and default duration configure kora jay.
Change korle abar build korte hobe.

## Bondho / uninstall
Tray > Exit. Tray > Start with Windows untick. Folder delete. (Registry value app nije muche dey.)

## Structure
- `Models/`      StatusOption, TimeOption, AwayInfo
- `ViewModels/`  MainViewModel (CommunityToolkit.Mvvm)
- `Views/`       MainWindow (popup)
- `Services/`    LockScreenImageRenderer, LockScreenRegistry, LockScreenService,
                 HotkeyService, SessionWatcher, StartupService, TrayService
- `THIRD-PARTY-NOTICES.txt`  Tabler Icons license

## Share korar age
- Unsigned .exe te Windows SmartScreen warning dekhate pare.
- App registry (HKLM) e likhe — office laptop e deowar age IT ke jaanan.
