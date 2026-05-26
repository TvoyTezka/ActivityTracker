using Microsoft.Data.Sqlite;

namespace TrackerApp;

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

    private void Migrate()
    {
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
                WHERE id = @id
                """;
            cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }

    public List<(string process, string category, long totalSeconds)> QueryReport(string? since)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            if (since != null)
            {
                cmd.CommandText = """
                    SELECT s.process, COALESCE(c.category, 'Uncategorized') as cat, SUM(s.duration_s) AS total_s
                    FROM sessions s
                    LEFT JOIN categories c ON s.process = c.process
                    WHERE s.started_at >= @since AND s.duration_s > 0
                    GROUP BY s.process, cat ORDER BY total_s DESC
                    """;
                cmd.Parameters.AddWithValue("@since", since);
            }
            else
            {
                cmd.CommandText = """
                    SELECT s.process, COALESCE(c.category, 'Uncategorized') as cat, SUM(s.duration_s) AS total_s
                    FROM sessions s
                    LEFT JOIN categories c ON s.process = c.process
                    WHERE s.duration_s > 0
                    GROUP BY s.process, cat ORDER BY total_s DESC
                    """;
            }

            var result = new List<(string, string, long)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetString(0), reader.GetString(1), reader.GetInt64(2)));
            return result;
        }
    }

    public List<(string day, long totalSeconds)> QueryDailyTotals(int days)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT date(started_at) AS day, SUM(duration_s) AS total_s
                FROM sessions WHERE started_at >= @since AND duration_s > 0
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
            cmd.CommandText = "SELECT COALESCE(SUM(duration_s), 0) FROM sessions WHERE duration_s > 0";
            return (long)cmd.ExecuteScalar()!;
        }
    }

    public List<(string process, string category)> QueryAllProcessMappings()
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT DISTINCT s.process, COALESCE(c.category, 'Uncategorized') as cat
                FROM sessions s
                LEFT JOIN categories c ON s.process = c.process
                ORDER BY s.process
                """;
            var result = new List<(string, string)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetString(0), reader.GetString(1)));
            return result;
        }
    }

    public void SaveCategoryMapping(string process, string category)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT OR REPLACE INTO categories (process, category)
                VALUES (@p, @c)
                """;
            cmd.Parameters.AddWithValue("@p", process);
            cmd.Parameters.AddWithValue("@c", category);
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose() => _conn.Dispose();
}
