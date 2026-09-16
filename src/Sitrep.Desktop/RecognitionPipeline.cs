using System.Drawing;
using Sitrep.Core;

namespace Sitrep.Desktop;

public static class RecognitionPipeline
{
    public static RecognitionResult Recognize(Bitmap roiImage, OcrEngine ocr)
    {
        var outputs = ocr.Recognize(roiImage);
        string raw = string.Join("\n--- recipe ---\n", outputs.Select(o => o.RawText));
        if (outputs.Any(o => !o.EngineUsed))
        {
            return new RecognitionResult(false, default, raw, 0, "OCR_UNAVAILABLE");
        }
        bool success = RecognitionConsensus.TryAccept(outputs.Select(o => o.RawText), out var coord, out string reason);
        return new RecognitionResult(success, coord, raw, outputs.Max(o => o.Confidence), reason);
    }

    public static RecognitionResult RecognizeImageFile(string imagePath, OcrEngine ocr, CaptureRegion? crop = null)
    {
        using var src = (Bitmap)Image.FromFile(imagePath);
        Bitmap roi = src;
        bool cloned = false;
        if (crop.HasValue)
        {
            var c = crop.Value;
            if (!RoiBuilder.IsSafeCapture(c, new CaptureRegion(0, 0, src.Width, src.Height), []))
            {
                return new RecognitionResult(false, default, string.Empty, 0, "BAD_CROP");
            }
            roi = src.Clone(new Rectangle(c.X, c.Y, c.Width, c.Height), src.PixelFormat);
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
