# Activity Tracker

Windows time tracker — records time spent in each foreground application.
Sits in system tray with a blue icon.

## Features

- **Automatic tracking** — polls active window every 2s via Win32 API
- **System tray** — double-click opens dashboard, right-click for menu
- **Interactive dashboard** — browser-based UI with donut charts, weekly bars, timeline
- **Categories** — group apps (Development, Browsing, Communication...) — editable live
- **Custom names** — give apps friendly display names (e.g. "Chrome" replaces `chrome.exe`)
- **Timeline** — day/week view of app sessions with timestamps
- **Idle detection** — pauses tracking after 2 min of inactivity
- **Pause/Resume** — toggle from tray menu
- **Auto-start** — toggle from tray menu (registry)
- **Settings** — configure poll interval & idle threshold
- **Backups** — automatic on start + manual via tray menu (keeps last 30)
- **Crash dump** — writes `%LocalAppData%\Tracker\crash.txt` on error

## Quick start

Run `TrackerApp\publish\TrackerApp.exe` or use the desktop shortcut.

| Action | Result |
|--------|--------|
| **Double-click** tray icon | Open dashboard in browser |
| **Right-click → Dashboard** | Charts, timeline, categories |
| **Right-click → Pause/Resume** | Stop/start tracking |
| **Right-click → Settings** | Poll interval, idle threshold |
| **Right-click → Backup now** | Manual DB backup |
| **Right-click → Auto-start** | Launch on boot |

## Data

- `%LocalAppData%\Tracker\tracker.db` — SQLite sessions database
- `%LocalAppData%\Tracker\config.json` — settings
- `%LocalAppData%\Tracker\backups\` — timestamped DB backups (max 30)

## Build

```powershell
dotnet publish -c Release -o publish
```

Requires .NET 10 SDK.

## Project structure

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point |
| `MainForm.cs` | Tray icon, menu, backup trigger |
| `TrackService.cs` | Foreground window polling |
| `Win32.cs` | P/Invoke (GetForegroundWindow, etc.) |
| `Database.cs` | SQLite storage, migrations, timeline queries |
| `Dashboard.cs` | HTML dashboard template + C# data builder |
| `WebServer.cs` | HTTP server serving dashboard + API |
| `ReportForm.cs` | WinForms table dialog |
| `SettingsForm.cs` | Poll interval & idle threshold UI |
| `Config.cs` | JSON config load/save |
| `BackupManager.cs` | DB backup & cleanup |
| `app.ico` | Application icon |
