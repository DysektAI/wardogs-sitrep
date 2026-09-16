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

    public static AppConfig Load(string? path = null)
    {
        path ??= ConfigPath;
        try
        {
            string json = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json)
                ?? throw new InvalidDataException("Configuration must be a JSON object.");
            cfg.Validate();
            return cfg;
        }
        catch (FileNotFoundException)
        {
            return new AppConfig();
        }
        catch (DirectoryNotFoundException)
        {
            return new AppConfig();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            throw new InvalidDataException($"Cannot load {path}: {ex.Message} Repair the file or rename it to restore defaults; it has not been overwritten.", ex);
        }
    }

    public void Validate()
    {
        if (RoiWidth is < 40 or > 2000 || RoiHeight is < 40 or > 2000
            || RoiOffsetX is < -2000 or > 2000 || RoiOffsetTop is < -2000 or > 2000)
        {
            throw new InvalidDataException("ROI width/height must be 40–2000 px and offsets -2000–2000 px.");
        }
        if (ForegroundTitleContains is null || string.IsNullOrWhiteSpace(TessDataDir)
            || !double.IsFinite(OverlayLeft) || !double.IsFinite(OverlayTop))
        {
            throw new InvalidDataException("Title match, tessdata path, and finite overlay positions are required.");
        }
    }

    public void Save(string? path = null)
    {
        Validate();
        path = Path.GetFullPath(path ?? ConfigPath);
        string directory = Path.GetDirectoryName(path)
            ?? throw new ArgumentException("Configuration path must name a file, not a filesystem root.", nameof(path));
        Directory.CreateDirectory(directory);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
