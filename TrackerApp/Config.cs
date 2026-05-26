using System.Text.Json;

namespace TrackerApp;

public class Config
{
    public int PollIntervalMs { get; set; } = 2000;
    public int IdleThresholdS { get; set; } = 120;

    private static string Path =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tracker", "config.json");

    public static Config Load()
    {
        try
        {
            if (File.Exists(Path))
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(Path)) ?? new Config();
        }
        catch { }
        return new Config();
    }

    public void Save()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
