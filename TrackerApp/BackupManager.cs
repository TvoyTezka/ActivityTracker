namespace TrackerApp;

public static class BackupManager
{
    private static readonly string BackupDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tracker", "backups");

    private static readonly string DbDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tracker");

    private static readonly string DbPath = System.IO.Path.Combine(DbDir, "tracker.db");
    private const int MaxBackups = 30;

    public static string? LastBackupTime { get; private set; }

    public static void AutoBackup()
    {
        try
        {
            Directory.CreateDirectory(BackupDir);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var dest = System.IO.Path.Combine(BackupDir, $"tracker_{timestamp}.db");
            CopyWithRetry(DbPath, dest);
            LastBackupTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            CleanOldBackups();
        }
        catch
        {
            // Silently skip backup on transient errors
        }
    }

    public static bool ManualBackup()
    {
        try
        {
            Directory.CreateDirectory(BackupDir);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var dest = System.IO.Path.Combine(BackupDir, $"tracker_{timestamp}.db");
            CopyWithRetry(DbPath, dest);
            LastBackupTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            CleanOldBackups();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CopyWithRetry(string src, string dest, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                File.Copy(src, dest, overwrite: true);
                return;
            }
            catch when (i < maxRetries - 1)
            {
                Thread.Sleep(200);
            }
        }
    }

    private static void CleanOldBackups()
    {
        try
        {
            var files = Directory.GetFiles(BackupDir, "tracker_*.db")
                .OrderByDescending(f => f)
                .ToList();

            if (files.Count > MaxBackups)
            {
                foreach (var f in files.Skip(MaxBackups))
                    File.Delete(f);
            }
        }
        catch { }
    }

    public static IEnumerable<string> ListBackups()
    {
        if (!Directory.Exists(BackupDir))
            return [];

        return Directory.GetFiles(BackupDir, "tracker_*.db")
            .OrderByDescending(f => f)
            .Select(f => System.IO.Path.GetFileName(f));
    }
}
