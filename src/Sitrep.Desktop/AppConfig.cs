using System.IO;
using System.Text.Json;

namespace Sitrep.Desktop;

public sealed class AppConfig
{
    public string ForegroundTitleContains { get; set; } = string.Empty;
    public bool DesktopTestMode { get; set; }
    public int RoiWidth { get; set; } = 360;
    public int RoiHeight { get; set; } = 200;
    public int RoiOffsetX { get; set; } = -40;
    public int RoiOffsetTop { get; set; } = 176;
    public bool DebugMode { get; set; }
    public string TessDataDir { get; set; } = "tessdata";
    public double OverlayLeft { get; set; } = -1;
    public double OverlayTop { get; set; } = -1;

    public static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sitrep");

    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg is not null)
                {
                    return cfg;
                }
            }
        }
        catch
        {
        }
        return new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
