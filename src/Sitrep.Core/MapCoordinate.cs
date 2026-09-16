namespace Sitrep.Core;

public readonly record struct MapCoordinate(double X, double Y)
{
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);
}
