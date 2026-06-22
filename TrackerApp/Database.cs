using Microsoft.Data.Sqlite;

namespace TrackerApp;

public class TimelineEntry
{
    public string Process { get; set; } = "";
    public string Category { get; set; } = "";
    public string? CustomName { get; set; }
    public string? WindowTitle { get; set; }
    public string StartedAt { get; set; } = "";
    public string? EndedAt { get; set; }
    public long Duration { get; set; }
}

public class Database : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly object _lock = new();

    public Database(string path)
    {
        _conn = new SqliteConnection($"Data Source={path}");
        _conn.Open();
        Migrate();
    }

    private void SetUserVersion(int version)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"PRAGMA user_version = {version};";
        cmd.ExecuteNonQuery();
    }

    private void Migrate()
    {
        // 1. Create base tables if they do not exist (fresh start)
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    process TEXT NOT NULL,
                    window_title TEXT NOT NULL,
                    started_at TEXT NOT NULL,
                    ended_at TEXT,
                    duration_s INTEGER DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_started ON sessions(started_at);
                CREATE TABLE IF NOT EXISTS categories (
                    process TEXT PRIMARY KEY,
                    category TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        // 2. Get user version
        int currentVersion = 0;
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA user_version;";
            currentVersion = Convert.ToInt32(cmd.ExecuteScalar());
        }

        // 3. Sequential migrations
        if (currentVersion < 1)
        {
            SetUserVersion(1);
            currentVersion = 1;
        }

        if (currentVersion < 2)
        {
            // Migration 2: Add custom_name TEXT to categories table
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE categories ADD COLUMN custom_name TEXT;";
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    // Ignore error if column already added manually
                }
            }
            SetUserVersion(2);
            currentVersion = 2;
        }

        // Insert default mappings if categories is empty
        lock (_lock)
        {
            using var countCmd = _conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM categories";
            var count = (long)countCmd.ExecuteScalar()!;
            if (count == 0)
            {
                var defaults = new Dictionary<string, string>
                {
                    { "code.exe", "Development" },
                    { "devenv.exe", "Development" },
                    { "rider64.exe", "Development" },
                    { "idea64.exe", "Development" },
                    { "unity.exe", "Development" },
                    { "git.exe", "Development" },
                    { "chrome.exe", "Browsing" },
                    { "firefox.exe", "Browsing" },
                    { "msedge.exe", "Browsing" },
                    { "opera.exe", "Browsing" },
                    { "telegram.exe", "Communication" },
                    { "discord.exe", "Communication" },
                    { "slack.exe", "Communication" },
                    { "spotify.exe", "Media" }
                };
                foreach (var kvp in defaults)
                {
                    using var insertCmd = _conn.CreateCommand();
                    insertCmd.CommandText = "INSERT OR IGNORE INTO categories (process, category) VALUES (@p, @c)";
                    insertCmd.Parameters.AddWithValue("@p", kvp.Key);
                    insertCmd.Parameters.AddWithValue("@c", kvp.Value);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }
    }

    public long StartSession(string process, string title)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO sessions (process, window_title, started_at) VALUES (@p, @t, @s); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@p", process);
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@s", DateTime.Now.ToString("o"));
            return (long)cmd.ExecuteScalar()!;
        }
    }

    public void EndSession(long id)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                UPDATE sessions SET ended_at = @now,
                    duration_s = CAST((julianday(@now) - julianday(started_at)) * 86400 AS INTEGER)
                WHERE id = @id;

                DELETE FROM sessions WHERE id = @id AND duration_s < 5;
                """;
            cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }

    public List<(string process, string category, string? customName, long totalSeconds)> QueryReport(string? since)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            if (since != null)
            {
                cmd.CommandText = """
                    SELECT s.process, COALESCE(c.category, 'Uncategorized') as cat, c.custom_name, SUM(s.duration_s) AS total_s
                    FROM sessions s
                    LEFT JOIN categories c ON s.process = c.process
                    WHERE s.started_at >= @since 
                      AND s.duration_s >= 5
                      AND (c.category IS NULL OR c.category != 'Ignored')
                    GROUP BY s.process, cat, c.custom_name 
                    ORDER BY total_s DESC
                    """;
                cmd.Parameters.AddWithValue("@since", since);
            }
            else
            {
                cmd.CommandText = """
                    SELECT s.process, COALESCE(c.category, 'Uncategorized') as cat, c.custom_name, SUM(s.duration_s) AS total_s
                    FROM sessions s
                    LEFT JOIN categories c ON s.process = c.process
                    WHERE s.duration_s >= 5
                      AND (c.category IS NULL OR c.category != 'Ignored')
                    GROUP BY s.process, cat, c.custom_name 
                    ORDER BY total_s DESC
                    """;
            }

            var result = new List<(string, string, string?, long)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetInt64(3)
                ));
            }
            return result;
        }
    }

    public List<(string day, long totalSeconds)> QueryDailyTotals(int days)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT date(s.started_at) AS day, SUM(s.duration_s) AS total_s
                FROM sessions s
                LEFT JOIN categories c ON s.process = c.process
                WHERE s.started_at >= @since 
                  AND s.duration_s >= 5
                  AND (c.category IS NULL OR c.category != 'Ignored')
                GROUP BY day ORDER BY day
                """;
            cmd.Parameters.AddWithValue("@since", DateTime.Now.AddDays(-days + 1).ToString("yyyy-MM-dd"));
            var result = new List<(string, long)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetString(0), reader.GetInt64(1)));
            return result;
        }
    }

    public long QueryGrandTotal()
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT COALESCE(SUM(s.duration_s), 0)
                FROM sessions s
                LEFT JOIN categories c ON s.process = c.process
                WHERE s.duration_s >= 5
                  AND (c.category IS NULL OR c.category != 'Ignored')
                """;
            return (long)cmd.ExecuteScalar()!;
        }
    }

    public List<(string process, string category, string? customName)> QueryAllProcessMappings()
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT DISTINCT s.process, COALESCE(c.category, 'Uncategorized') as cat, c.custom_name
                FROM sessions s
                LEFT JOIN categories c ON s.process = c.process
                ORDER BY s.process
                """;
            var result = new List<(string, string, string?)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)
                ));
            }
            return result;
        }
    }

    public void SaveProcessSettings(string process, string? category, string? customName)
    {
        lock (_lock)
        {
            // 1. Insert default settings if process row is not present
            using (var insertCmd = _conn.CreateCommand())
            {
                insertCmd.CommandText = "INSERT OR IGNORE INTO categories (process, category) VALUES (@p, 'Uncategorized')";
                insertCmd.Parameters.AddWithValue("@p", process);
                insertCmd.ExecuteNonQuery();
            }

            // 2. Perform updates for fields that are provided
            using (var updateCmd = _conn.CreateCommand())
            {
                if (category != null && customName != null)
                {
                    updateCmd.CommandText = "UPDATE categories SET category = @c, custom_name = @n WHERE process = @p";
                    updateCmd.Parameters.AddWithValue("@c", category);
                    updateCmd.Parameters.AddWithValue("@n", customName);
                }
                else if (category != null)
                {
                    updateCmd.CommandText = "UPDATE categories SET category = @c WHERE process = @p";
                    updateCmd.Parameters.AddWithValue("@c", category);
                }
                else if (customName != null)
                {
                    updateCmd.CommandText = "UPDATE categories SET custom_name = @n WHERE process = @p";
                    updateCmd.Parameters.AddWithValue("@n", customName);
                }
                else
                {
                    return; // nothing to update
                }
                updateCmd.Parameters.AddWithValue("@p", process);
                updateCmd.ExecuteNonQuery();
            }
        }
    }

    public List<TimelineEntry> QueryTimeline(string startDay, string endDay)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT s.process, COALESCE(c.category, 'Uncategorized') as cat, c.custom_name, s.window_title, s.started_at, s.ended_at, s.duration_s
                FROM sessions s
                LEFT JOIN categories c ON s.process = c.process
                WHERE date(s.started_at) >= @start
                  AND date(s.started_at) <= @end
                  AND s.duration_s >= 5
                  AND (c.category IS NULL OR c.category != 'Ignored')
                ORDER BY s.started_at ASC
                """;
            cmd.Parameters.AddWithValue("@start", startDay);
            cmd.Parameters.AddWithValue("@end", endDay);
            var result = new List<TimelineEntry>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new TimelineEntry
                {
                    Process = reader.GetString(0),
                    Category = reader.GetString(1),
                    CustomName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    WindowTitle = reader.IsDBNull(3) ? null : reader.GetString(3),
                    StartedAt = reader.GetString(4),
                    EndedAt = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Duration = reader.GetInt64(6)
                });
            }
            return result;
        }
    }

    public void Dispose() => _conn.Dispose();
}
