using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using Sitrep.Core;

namespace Sitrep.Desktop;

internal static class DebugCaptureStore
{
    internal static void Save(CaptureRequest request, Bitmap image, RecognitionResult result, string? directory = null)
    {
        string dir = directory ?? Path.Combine(AppConfig.ConfigDir, "captures");
        try
        {
            Directory.CreateDirectory(dir);
            // Retire the legacy unbounded log. New captures use bounded image/record pairs.
            File.Delete(Path.Combine(dir, "records.log"));
            var files = new DirectoryInfo(dir).GetFiles()
                .Where(f => f.Extension is ".png" or ".json")
                .OrderBy(f => f.CreationTimeUtc).ThenBy(f => f.Name).ToList();
            while (files.Count > 48)
            {
                files[0].Delete();
                files.RemoveAt(0);
            }
            string name = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}_{request.Role}_{request.Sequence}";
            image.Save(Path.Combine(dir, name + ".png"), ImageFormat.Png);
            File.WriteAllText(Path.Combine(dir, name + ".json"), JsonSerializer.Serialize(new
            {
                request,
                result,
                profile = "L81 Apollyon, uncorrected table",
                completedAt = DateTimeOffset.UtcNow,
            }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
        {
            // Debug persistence is best effort, never a condition for a valid OCR result.
            System.Diagnostics.Trace.WriteLine($"Debug capture not saved: {ex.Message}");
        }
    }
}
