using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;

namespace Sitrep.Desktop;

public sealed record OcrOutput(string RawText, float Confidence, bool EngineUsed);

public sealed class OcrEngine : IDisposable
{
    private TesseractEngine? _engine;
    private bool _disposed;

    public string TessDataPath { get; }
    public string InitError { get; private set; } = string.Empty;
    public bool IsReady => _engine is not null;

    public OcrEngine(string tessDataPath)
    {
        TessDataPath = tessDataPath;
    }

    public bool TryInit(out string error)
    {
        error = string.Empty;
        try
        {
            if (!File.Exists(Path.Combine(TessDataPath, "eng.traineddata")))
            {
                error = $"Missing eng.traineddata in {TessDataPath}. Run scripts/setup-model.ps1.";
                InitError = error;
                return false;
            }
            _engine = new TesseractEngine(TessDataPath, "eng", EngineMode.LstmOnly);
            _engine.SetVariable("tessedit_pageseg_mode", "11");
            _engine.SetVariable("tessedit_char_whitelist", "xyXY0123456789., ");
            return true;
        }
        catch (Exception ex)
        {
            error = $"OCR init failed: {ex.GetType().Name}: {ex.Message}. Ensure VC++ x64 runtime is installed.";
            InitError = error;
            return false;
        }
    }

    public OcrOutput Recognize(Bitmap image)
    {
        if (_engine is null)
        {
            return new OcrOutput(string.Empty, 0, false);
        }
        string best = string.Empty;
        float bestConf = 0;
        foreach (bool threshold in new[] { false, true })
        {
            using var pre = ScreenCapture.PreprocessForOcr((Bitmap)image.Clone(), threshold);
            using var ms = new MemoryStream();
            pre.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            using var pix = Pix.LoadFromMemory(ms.ToArray());
            using var page = _engine.Process(pix, PageSegMode.SparseText);
            string text = page.GetText() ?? string.Empty;
            float conf = page.GetMeanConfidence();
            if (text.Length > best.Length)
            {
                best = text;
                bestConf = conf;
            }
            if (!string.IsNullOrWhiteSpace(text) && text.Contains('.'))
            {
                break;
            }
        }
        return new OcrOutput(best, bestConf, true);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _engine?.Dispose();
        _engine = null;
    }
}
