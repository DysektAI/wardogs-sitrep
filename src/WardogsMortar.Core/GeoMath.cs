namespace WardogsMortar.Core;

public static class GeoMath
{
    public const double MetersPerUnit = 100.0;

    public static bool TryCompute(MapCoordinate origin, MapCoordinate target, out double rangeMeters, out double bearingDegrees, out string rejectionReason)
    {
        rangeMeters = double.NaN;
        bearingDegrees = double.NaN;
        rejectionReason = string.Empty;
        if (!origin.IsFinite || !target.IsFinite)
        {
            rejectionReason = "NON_FINITE_INPUT";
            return false;
        }
        double dx = (target.X - origin.X) * MetersPerUnit;
        double dy = (target.Y - origin.Y) * MetersPerUnit;
        if (!double.IsFinite(dx) || !double.IsFinite(dy))
        {
            rejectionReason = "NON_FINITE_INPUT";
            return false;
        }
        double range = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(range))
        {
            rejectionReason = "NON_FINITE_INPUT";
            return false;
        }
        rangeMeters = range;
        if (range == 0.0)
        {
            rejectionReason = "ZERO_RANGE";
            return false;
        }
        double bearing = Math.Atan2(dx, dy) * 180.0 / Math.PI;
        bearing %= 360.0;
        if (bearing < 0)
        {
            bearing += 360.0;
        }
        if (bearing >= 360.0)
        {
            bearing -= 360.0;
        }
        bearingDegrees = bearing;
        return true;
    }

    public static double NormalizeBearing(double degrees)
    {
        double b = degrees % 360.0;
        if (b < 0)
        {
            b += 360.0;
        }
        if (b >= 360.0)
        {
            b -= 360.0;
        }
        return b;
    }
}
