namespace Sitrep.Core;

public static class OverlayPlacement
{
    /// <summary>Keeps a window of <paramref name="size"/> inside [origin, origin + extent]; pins to origin when it cannot fit.</summary>
    public static double Clamp(double value, double origin, double extent, double size) =>
        Math.Clamp(value, origin, Math.Max(origin, origin + extent - size));
}
