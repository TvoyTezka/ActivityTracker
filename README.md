# Activity Tracker

Windows time tracker — records time spent in each foreground application.  
Sits quietly in the system tray.

## Quick start

Run `publish/TrackerApp.exe`. A blue icon appears in the tray.

| Action | Result |
|--------|--------|
| **Double-click** tray icon | Open HTML dashboard |
| **Right-click → Dashboard** | Dashboard with charts |
| **Right-click → Today / Week / All time** | Table report |
| **Right-click → Auto-start with Windows** | Launch on boot |
| **Right-click → Exit** | Stop and close |

In Task Manager the app shows as **Activity Tracker** with its icon.

## Data

`%LocalAppData%\Tracker\tracker.db` (SQLite)

## Auto-start

Toggle via tray menu → writes to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

## Build

```powershell
dotnet publish -c Release -o publish
```

Requires .NET 10 SDK.

## Project

| File | Role |
|------|------|
| `Program.cs` | Entry point |
| `MainForm.cs` | Tray icon, menu, auto-start |
| `TrackService.cs` | Foreground window polling |
| `Win32.cs` | Win32 P/Invoke |
| `Database.cs` | SQLite |
| `ReportForm.cs` | Table dialog |
| `Dashboard.cs` | HTML dashboard |
| `app.ico` | Icon (16/32/48/256 px) |
