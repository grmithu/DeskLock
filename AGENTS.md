# Repository Guidelines

## Project Structure & Module Organization

DeskLock is a Windows-only WPF application targeting `net10.0-windows`. Startup and shared configuration live in `App.xaml`, `App.xaml.cs`, and `AppSettings.cs`. Keep UI markup and code-behind in `Views/`, presentation logic in `ViewModels/`, data types in `Models/`, and Windows integration or rendering in `Services/`. The solution and project are `DeskLock.sln` and `DeskLock.csproj`. There is no test project. `bin/` and `obj/` are generated output; do not commit them.

## Build, Run, and Development Commands

Use PowerShell on Windows with the .NET 10 SDK. The app manifest requests administrator privileges; Windows integration may require elevation.

- `dotnet restore .\DeskLock.sln` restores NuGet dependencies.
- `dotnet build .\DeskLock.sln -c Release` builds the solution.
- `dotnet run --project .\DeskLock.csproj` launches the tray application.
- `dotnet run --project .\DeskLock.csproj -- --render-themes` renders theme previews without locking the workstation.
- `dotnet publish .\DeskLock.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true` publishes a single-file Windows build requiring the .NET Desktop Runtime.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, file-scoped namespaces, `PascalCase` for types and public members, and `_camelCase` for private fields. Keep nullable reference types enabled and implicit usings disabled; add explicit `using` directives. Use descriptive XAML names consistent with view-model properties. Keep status defaults and themes in `AppSettings.cs`. No formatter or linter is configured, so match nearby code.

## Testing Guidelines

There is no automated test suite or coverage requirement. Build changes with `dotnet build .\DeskLock.sln -c Release`. Manually verify UI, rendering, or Windows integration changes on Windows; `--render-themes` generates previews without applying a lock screen. Compilation alone does not verify registry, hotkey, tray, or session behavior.

## Commit & Pull Request Guidelines

Recent commits use short imperative subjects, such as `Add custom message theme` and `Enhance focus handling`. Follow that style and keep commits focused. Pull requests should explain user-visible changes, note relevant Windows requirements, link related issues, include screenshots for UI changes, and describe build or manual verification.

## Configuration & Generated Data

Built-in statuses, themes, and time options are configured in `AppSettings.cs`. Runtime images and logs are stored under `%APPDATA%\DeskLock\`; do not add these user-specific files to the repository. See `THIRD-PARTY-NOTICES.txt` before changing icon assets derived from Tabler Icons.
