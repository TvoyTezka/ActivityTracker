using Microsoft.Win32;

namespace TrackerApp;

public class MainForm : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly TrackService _tracker;
    private readonly Database _db;
    private readonly WebServer _webServer;
    private readonly ToolStripMenuItem _pauseItem;
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TrackerApp";

    public MainForm()
    {
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        Visible = false;

        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tracker", "tracker.db");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
        _db = new Database(dbPath);
        _tracker = new TrackService(_db);
        _webServer = new WebServer(_db);
        _webServer.Start();
        Dashboard.Port = _webServer.Port;

        BackupManager.AutoBackup();
        var lastBackup = BackupManager.LastBackupTime ?? "Never";
        var backupLabel = $"Last backup: {lastBackup}";

        var autoStartItem = new ToolStripMenuItem("Auto-start with Windows",
            null, OnToggleAutoStart)
        { Checked = IsAutoStartEnabled() };

        _pauseItem = new ToolStripMenuItem("Pause tracking", null, OnTogglePause);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Dashboard", null, (_, _) => Dashboard.Open());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_pauseItem);
        menu.Items.Add("Settings...", null, (_, _) => new SettingsForm(_tracker).Show());
        menu.Items.Add("Backup now", null, OnBackup);
        menu.Items.Add(new ToolStripMenuItem(backupLabel) { Enabled = false });
        menu.Items.Add(autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, OnExit);

        _trayIcon = new NotifyIcon
        {
            Icon = MakeIcon(),
            Text = "Activity Tracker",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => Dashboard.Open();

        _tracker.Start();
    }

    private void OnTogglePause(object? sender, EventArgs e)
    {
        _tracker.TogglePause();
        _pauseItem.Text = _tracker.Paused ? "Resume tracking" : "Pause tracking";
        _trayIcon.Text = _tracker.Paused ? "Activity Tracker (paused)" : "Activity Tracker";
    }

    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }

    private static void OnToggleAutoStart(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item) return;

        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;

        if (IsAutoStartEnabled())
            key.DeleteValue(AppName, throwOnMissingValue: false);
        else
            key.SetValue(AppName, Application.ExecutablePath);

        item.Checked = IsAutoStartEnabled();
    }

    private static Icon MakeIcon()
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.FromArgb(99, 102, 241));
        g.FillEllipse(brush, 2, 2, 28, 28);
        g.FillEllipse(Brushes.White, 7, 7, 18, 18);
        var pts = new[] { new Point(14, 10), new Point(14, 22), new Point(20, 16) };
        g.FillPolygon(brush, pts);
        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private void OnBackup(object? sender, EventArgs e)
    {
        if (BackupManager.ManualBackup())
        {
            _trayIcon.ShowBalloonTip(2000, "Backup", $"Backup saved: {BackupManager.LastBackupTime}", ToolTipIcon.Info);
        }
        else
        {
            _trayIcon.ShowBalloonTip(2000, "Backup failed", "Could not create backup. Check disk space.", ToolTipIcon.Error);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _tracker.Stop();
        _webServer.Stop();
        _trayIcon.Visible = false;
        _db.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tracker.Stop();
            _webServer.Stop();
            _trayIcon.Dispose();
            _db.Dispose();
        }
        base.Dispose(disposing);
    }
}
