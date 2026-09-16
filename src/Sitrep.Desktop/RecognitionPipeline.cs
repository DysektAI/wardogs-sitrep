using System.Drawing;
using Sitrep.Core;

namespace Sitrep.Desktop;

public sealed record RecognitionResult(bool Success, MapCoordinate Coordinate, string RawText, float Confidence, string RejectionReason);

public static class RecognitionPipeline
{
    public static RecognitionResult Recognize(Bitmap roiImage, OcrEngine ocr)
    {
        var output = ocr.Recognize(roiImage);
        string raw = output.RawText ?? string.Empty;
        if (!output.EngineUsed)
        {
            return new RecognitionResult(false, default, raw, 0, "OCR_UNAVAILABLE");
        }
        if (CoordinateParser.TryParse(raw, out var coord, out string reason))
        {
            return new RecognitionResult(true, coord, raw, output.Confidence, string.Empty);
        }
        return new RecognitionResult(false, default, raw, output.Confidence, reason);
    }

    public static RecognitionResult RecognizeImageFile(string imagePath, OcrEngine ocr, CaptureRegion? crop = null)
    {
        using var src = (Bitmap)Image.FromFile(imagePath);
        Bitmap roi = src;
        bool cloned = false;
        if (crop.HasValue && !crop.Value.IsEmpty)
        {
            var c = crop.Value;
            int x = Math.Clamp(c.X, 0, src.Width - 1);
            int y = Math.Clamp(c.Y, 0, src.Height - 1);
            int w = Math.Min(c.Width, src.Width - x);
            int h = Math.Min(c.Height, src.Height - y);
            if (w <= 0 || h <= 0)
            {
                return new RecognitionResult(false, default, string.Empty, 0, "BAD_CROP");
            }
            roi = src.Clone(new Rectangle(x, y, w, h), src.PixelFormat);
            cloned = true;
        }
        try
        {
            return Recognize(roi, ocr);
        }
        finally
        {
            if (cloned)
            {
                roi.Dispose();
            }
        }
    }
}
