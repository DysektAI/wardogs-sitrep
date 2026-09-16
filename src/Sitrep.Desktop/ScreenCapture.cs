using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Sitrep.Core;

namespace Sitrep.Desktop;

public static class ScreenCapture
{
    public static CaptureRegion BuildRoi(int cursorX, int cursorY, AppConfig cfg) =>
        RoiBuilder.BuildCursorRelative(cursorX, cursorY, cfg.RoiWidth, cfg.RoiHeight, cfg.RoiOffsetX, cfg.RoiOffsetTop);

    public static Bitmap? CaptureRegion(CaptureRegion roi, CaptureRegion? bounds, IReadOnlyList<CaptureRegion> exclude)
    {
        if (!RoiBuilder.IsSafeCapture(roi, bounds, exclude))
        {
            return null;
        }
        var bmp = new Bitmap(roi.Width, roi.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(roi.X, roi.Y, 0, 0, new Size(roi.Width, roi.Height), CopyPixelOperation.SourceCopy);
            return bmp;
        }
        catch
        {
            bmp.Dispose();
            throw;
        }
    }

    // Borrows src; caller owns the returned bitmap. No per-pixel GDI calls or hidden source disposal.
    public static Bitmap PreprocessForOcr(Bitmap src, bool threshold)
    {
        using var scaled = new Bitmap(src.Width * 2, src.Height * 2, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, scaled.Width, scaled.Height);
        }
        var output = new Bitmap(scaled.Width + 20, scaled.Height + 20, PixelFormat.Format32bppArgb);
        try
        {
            using (var g = Graphics.FromImage(output))
            {
                g.Clear(Color.Black);
                g.DrawImageUnscaled(scaled, 10, 10);
            }
            var data = output.LockBits(new Rectangle(0, 0, output.Width, output.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                byte[] row = new byte[output.Width * 4];
                for (int y = 0; y < output.Height; y++)
                {
                    var address = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(address, row, 0, row.Length);
                    for (int x = 0; x < row.Length; x += 4)
                    {
                        int lum = (299 * row[x + 2] + 587 * row[x + 1] + 114 * row[x]) / 1000;
                        byte value = (byte)(threshold ? (lum > 140 ? 255 : 0) : lum);
                        row[x] = row[x + 1] = row[x + 2] = value;
                    }
                    Marshal.Copy(row, 0, address, row.Length);
                }
            }
            finally
            {
                output.UnlockBits(data);
            }
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }
}
