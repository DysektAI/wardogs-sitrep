using System.Drawing;
using System.Drawing.Imaging;
using WardogsMortar.Core;

namespace WardogsMortar.Desktop;

public sealed record CapturedFrame(Bitmap Image, int CursorX, int CursorY, CaptureRegion Roi, long ForegroundHwnd, DateTimeOffset EventTime);

public static class ScreenCapture
{
    public static CaptureRegion BuildRoi(int cursorX, int cursorY, AppConfig cfg) =>
        RoiBuilder.BuildCursorRelative(cursorX, cursorY, cfg.RoiWidth, cfg.RoiHeight, cfg.RoiOffsetX, cfg.RoiOffsetTop);

    public static CapturedFrame? CaptureCursorRegion(int cursorX, int cursorY, CaptureRegion roi, IntPtr foregroundHwnd, IReadOnlyList<CaptureRegion> exclude)
    {
        if (roi.IsEmpty)
        {
            return null;
        }
        foreach (var ex in exclude)
        {
            if (RoiBuilder.Overlaps(roi, ex))
            {
                return null;
            }
        }
        try
        {
            var bmp = new Bitmap(roi.Width, roi.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(roi.X, roi.Y, 0, 0, new Size(roi.Width, roi.Height), CopyPixelOperation.SourceCopy);
            }
            return new CapturedFrame(bmp, cursorX, cursorY, roi, foregroundHwnd.ToInt64(), DateTimeOffset.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    public static Bitmap PreprocessForOcr(Bitmap src, bool threshold)
    {
        int scale = 2;
        var scaled = new Bitmap(src.Width * scale, src.Height * scale, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, scaled.Width, scaled.Height);
        }
        var outBmp = new Bitmap(scaled.Width + 20, scaled.Height + 20, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(outBmp))
        {
            g.Clear(Color.Black);
            g.DrawImage(scaled, 10, 10);
        }
        scaled.Dispose();
        if (!threshold)
        {
            return ToGrayscale(outBmp);
        }
        var gray = ToGrayscale(outBmp);
        var thr = new Bitmap(gray.Width, gray.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < gray.Height; y++)
        {
            for (int x = 0; x < gray.Width; x++)
            {
                Color c = gray.GetPixel(x, y);
                int v = c.R > 140 ? 255 : 0;
                thr.SetPixel(x, y, Color.FromArgb(v, v, v));
            }
        }
        gray.Dispose();
        outBmp.Dispose();
        return thr;
    }

    private static Bitmap ToGrayscale(Bitmap src)
    {
        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                Color c = src.GetPixel(x, y);
                int lum = (299 * c.R + 587 * c.G + 114 * c.B) / 1000;
                dst.SetPixel(x, y, Color.FromArgb(c.A, lum, lum, lum));
            }
        }
        src.Dispose();
        return dst;
    }
}
