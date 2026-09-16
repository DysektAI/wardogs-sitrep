namespace Sitrep.Core;

public readonly record struct CaptureRegion(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public static class RoiBuilder
{
    public const int DefaultWidth = 360;
    public const int DefaultHeight = 200;
    public const int DefaultOffsetX = -40;
    public const int DefaultOffsetBottom = 24;
    public const int DefaultOffsetTop = 176;

    public static CaptureRegion BuildCursorRelative(int cursorX, int cursorY, int width = DefaultWidth, int height = DefaultHeight, int offsetX = DefaultOffsetX, int offsetTop = DefaultOffsetTop)
    {
        int x = cursorX + offsetX;
        int y = cursorY - offsetTop;
        return new CaptureRegion(x, y, width, height);
    }

    public static CaptureRegion? Intersect(CaptureRegion roi, CaptureRegion bounds)
    {
        int x1 = Math.Max(roi.X, bounds.X);
        int y1 = Math.Max(roi.Y, bounds.Y);
        int x2 = Math.Min(roi.X + roi.Width, bounds.X + bounds.Width);
        int y2 = Math.Min(roi.Y + roi.Height, bounds.Y + bounds.Height);
        if (x2 <= x1 || y2 <= y1)
        {
            return null;
        }
        return new CaptureRegion(x1, y1, x2 - x1, y2 - y1);
    }

    public static bool Overlaps(CaptureRegion a, CaptureRegion b) =>
        a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
}
