# DeskLock

DeskLock is a Windows desktop utility for setting an away status and expected return time before locking your workstation. It runs in the system tray and applies the selected information to a generated Windows lock-screen image.

## Table of contents

- [Overview](#overview)
- [Features](#features)
- [Keyboard shortcuts](#keyboard-shortcuts)
- [Technology stack](#technology-stack)
- [Architecture](#architecture)
- [Status options](#status-options)
- [Folder structure](#folder-structure)
- [Prerequisites](#prerequisites)
- [Install and set up](#install-and-set-up)
- [Run](#run)
- [Build](#build)
- [Publish](#publish)
- [Generate theme previews](#generate-theme-previews)
- [Data and configuration](#data-and-configuration)

## Overview

DeskLock opens a compact status dialog from a global keyboard shortcut or the tray icon. Choose a predefined status or enter a custom message, select an expected return time, and apply it. DeskLock renders the message and return time into an image, sets the Windows lock-screen image through the Personalization CSP registry values, and locks the workstation.

DeskLock is a Windows-only WPF application targeting `net10.0-windows`. Its application manifest requests administrator privileges.

## Features

- Eight predefined away statuses, each with a description, icon, color theme, and default return duration.
- A custom away message with its own lock-screen theme.
- Return-time options of No ETA, 10 minutes, 15 minutes, 30 minutes, or 1 hour, plus a clock-style picker for an exact time later that day.
- A generated lock-screen image containing the chosen message and, when set, the return time.
- A system-tray icon and menu for opening the status dialog, enabling or disabling startup, and exiting the app.
- A global `Ctrl+Alt+L` shortcut to open the status dialog.
- First-run setup of a Windows Task Scheduler logon task. If startup setup fails, DeskLock remains running and shows a notification.
- Removal of the applied lock-screen registry values when the current session unlocks or logs on.
- A theme-preview command that renders preview images without locking the workstation.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl+Alt+L` | Open the status dialog. |
| `Enter` | Apply the selected status or custom message and lock the workstation. |
| `Esc` | Close the dialog without locking; DeskLock remains in the tray. |
| Arrow keys | Move between and select items in the status or return-time option grids when a grid item is focused. |

The global hotkey may be unavailable if another application has already registered it. In that case, DeskLock reports the issue in a tray notification; open the dialog from the tray icon instead.

## Technology stack

- C# and .NET 10 (`net10.0-windows`)
- Windows Presentation Foundation (WPF) for the dialog and image rendering
- Windows Forms `NotifyIcon` for the system-tray icon and menu
- CommunityToolkit.Mvvm 8.4.0 for observable view-model properties and commands
- Windows facilities accessed through .NET and P/Invoke, including the registry, Task Scheduler (`schtasks.exe`), session notifications, global hotkeys, and workstation locking

## Architecture

- `App.xaml` and `App.xaml.cs` manage application startup, the single-instance behavior, the tray, the global hotkey, and session monitoring.
- `Views/MainWindow.xaml` and its code-behind implement the status dialog, its time picker, and keyboard navigation.
- `ViewModels/MainViewModel.cs` manages status and time selection, validation, and the apply-and-lock workflow.
- `Models/` contains the status, time-option, and away-information models.
- `Services/` contains the lock-screen image renderer and registry integration, workstation locking, hotkey, session, startup-task, and tray services.
- `AppSettings.cs` defines the built-in statuses, themes, default status, and time options.

When applying a status, DeskLock renders a PNG for the primary display, sets Windows' Personalization CSP lock-screen registry values, hides the dialog, and calls the Windows workstation-lock API. Lock-screen rendering and registry errors are written to the app's `errors.log`.

## Status options

| Status | Description | Default return time |
| --- | --- | --- |
| Break | Short break from work | 10 minutes |
| Lunch | Having lunch | 30 minutes |
| Prayer | Going for prayer | 10 minutes |
| Meeting | Attending a meeting | 1 hour |
| Away from Desk | Away from my desk | No ETA |
| On Another Floor | Visiting another floor | No ETA |
| Refreshment Break | Tea, coffee, or snacks | No ETA |
| Outside Office | Outside the office | 1 hour |

Selecting a status selects its default duration unless a return time has already been chosen in the current dialog. The exact-time picker accepts a future time today. A custom message uses a separate built-in theme.

## Folder structure

```text
DeskLock/
├── Models/                 Status, time-option, and away-information models
├── Services/               Windows integration, rendering, tray, and startup services
├── ViewModels/             Main dialog view-model
├── Views/                  WPF status dialog
├── App.xaml                WPF application definition
├── App.xaml.cs             Startup and application lifecycle
├── AppSettings.cs          Built-in status and time configuration
├── app.manifest            Requests administrator privileges
├── DeskLock.csproj         Project, target framework, and package reference
├── DeskLock.sln            Visual Studio solution
└── THIRD-PARTY-NOTICES.txt Tabler Icons license notice
```

`bin/` and `obj/` are generated by builds and are not source folders.

## Prerequisites

- Windows with support for .NET 10 desktop applications.
- The .NET 10 SDK to restore, build, and run from source.
- An administrator account and an elevated terminal to run DeskLock. The application manifest requests elevation, and the lock-screen integration writes to `HKEY_LOCAL_MACHINE`.

## Install and set up

Get the project source, then open **PowerShell as Administrator** and change to the project directory:

```powershell
Set-Location E:\Projects\DeskLock
dotnet restore .\DeskLock.sln
```

Run DeskLock elevated. On its first successful launch, it attempts to configure a Task Scheduler task to start the app at the current user's logon. Use **Start with Windows** in the tray menu to turn that task off or on later.

## Run

From an elevated PowerShell window in the project directory:

```powershell
dotnet run --project .\DeskLock.csproj
```

The dialog does not open automatically. Use `Ctrl+Alt+L` or click the tray icon. Selecting **Exit** in the tray menu closes DeskLock; closing or cancelling the dialog only hides it.

## Build

Build the solution from an elevated PowerShell window:

```powershell
dotnet build .\DeskLock.sln -c Release
```

The project output is written beneath `bin\Release\net10.0-windows\`.

## Publish

Publish a single-file, framework-dependent executable for 64-bit Windows:

```powershell
dotnet publish .\DeskLock.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

The target computer must have the .NET 10 Desktop Runtime installed.

To publish a self-contained executable that includes the .NET runtime:

```powershell
dotnet publish .\DeskLock.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The publish output is in `bin\Release\net10.0-windows\win-x64\publish\`. Run the published executable elevated; its manifest will request administrator approval.

## Generate theme previews

Render PNG previews for the built-in statuses and custom-message theme without locking the workstation:

```powershell
dotnet run --project .\DeskLock.csproj -- --render-themes
```

Run this command elevated, as the application manifest still requests administrator privileges. The images are written to `%APPDATA%\DeskLock\previews\`.

## Data and configuration

- `AppSettings.cs` contains the built-in status names, descriptions, themes, default durations, and default selected status. Changing these source values requires rebuilding the app.
- The active lock-screen image is written to `%APPDATA%\DeskLock\lock.png`.
- Theme previews are written to `%APPDATA%\DeskLock\previews\`.
- Lock-screen rendering and registry failures are recorded in `%APPDATA%\DeskLock\errors.log`.
- DeskLock clears its configured lock-screen registry values on session unlock or logon and when the running app exits.
- The outline icon geometries are derived from Tabler Icons; see `THIRD-PARTY-NOTICES.txt` for the license.
