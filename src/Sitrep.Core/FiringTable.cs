using System.Text.Json;

namespace Sitrep.Core;

public sealed record FiringSample(double RangeMeters, double ElevationMil);

public sealed class FiringTable
{
    public string WeaponId { get; }
    public double MinRangeMeters { get; }
    public double MaxRangeMeters { get; }
    public IReadOnlyList<FiringSample> Samples { get; }

    private FiringTable(string weaponId, double minRangeMeters, double maxRangeMeters, IEnumerable<FiringSample> samples)
    {
        WeaponId = weaponId;
        MinRangeMeters = minRangeMeters;
        MaxRangeMeters = maxRangeMeters;
        Samples = samples.OrderBy(s => s.RangeMeters).ToList().AsReadOnly();
    }

    public static (FiringTable? Table, string? Error) TryCreate(string weaponId, double minRangeMeters, double maxRangeMeters, IEnumerable<FiringSample> samples)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return (null, "MISSING_WEAPON_ID");
        }
        if (!double.IsFinite(minRangeMeters) || !double.IsFinite(maxRangeMeters) || minRangeMeters <= 0 || maxRangeMeters <= 0 || minRangeMeters >= maxRangeMeters)
        {
            return (null, "INVALID_LIMITS");
        }
        var ordered = samples.ToList();
        if (ordered.Count < 2)
        {
            return (null, "TOO_FEW_SAMPLES");
        }
        for (int i = 0; i < ordered.Count; i++)
        {
            if (!double.IsFinite(ordered[i].RangeMeters) || !double.IsFinite(ordered[i].ElevationMil))
            {
                return (null, "NON_FINITE_SAMPLE");
            }
            if (ordered[i].RangeMeters <= 0 || ordered[i].ElevationMil <= 0 || ordered[i].ElevationMil >= 6400)
            {
                return (null, "INVALID_UNITS");
            }
            if (i > 0 && ordered[i].RangeMeters <= ordered[i - 1].RangeMeters)
            {
                return (null, "DUPLICATE_OR_UNORDERED_RANGE");
            }
        }
        var table = new FiringTable(weaponId, minRangeMeters, maxRangeMeters, ordered);
        return (table, null);
    }

    public bool IsInWeaponLimits(double rangeMeters) =>
        double.IsFinite(rangeMeters) && rangeMeters >= MinRangeMeters && rangeMeters <= MaxRangeMeters;

    public bool IsInTableCoverage(double rangeMeters) =>
        Samples.Count >= 2 && double.IsFinite(rangeMeters) &&
        rangeMeters >= Samples[0].RangeMeters && rangeMeters <= Samples[^1].RangeMeters;

    public bool TryInterpolate(double rangeMeters, out double elevationMil, out string rejectionReason)
    {
        elevationMil = double.NaN;
        rejectionReason = string.Empty;
        if (!double.IsFinite(rangeMeters))
        {
            rejectionReason = "NON_FINITE_RANGE";
            return false;
        }
        if (!IsInWeaponLimits(rangeMeters))
        {
            rejectionReason = "OUT_OF_RANGE";
            return false;
        }
        if (!IsInTableCoverage(rangeMeters))
        {
            rejectionReason = "OUT_OF_TABLE";
            return false;
        }
        for (int i = 0; i < Samples.Count; i++)
        {
            if (rangeMeters == Samples[i].RangeMeters)
            {
                elevationMil = Samples[i].ElevationMil;
                return true;
            }
        }
        for (int i = 1; i < Samples.Count; i++)
        {
            double r0 = Samples[i - 1].RangeMeters;
            double r1 = Samples[i].RangeMeters;
            if (rangeMeters > r0 && rangeMeters < r1)
            {
                double m0 = Samples[i - 1].ElevationMil;
                double m1 = Samples[i].ElevationMil;
                double t = (rangeMeters - r0) / (r1 - r0);
                elevationMil = m0 + t * (m1 - m0);
                return true;
            }
        }
        rejectionReason = "OUT_OF_TABLE";
        return false;
    }

    public static (FiringTable? Table, string? Error) LoadApollyonL81(string jsonPath)
    {
        try
        {
            string json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string weaponId = root.GetProperty("weaponId").GetString() ?? string.Empty;
            double minRange = root.GetProperty("minRangeMeters").GetDouble();
            double maxRange = root.GetProperty("maxRangeMeters").GetDouble();
            List<FiringSample> samples = new();
            foreach (var el in root.GetProperty("samples").EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Array || el.GetArrayLength() != 2)
                {
                    return (null, "CORRUPT_DATA");
                }
                samples.Add(new FiringSample(el[0].GetDouble(), el[1].GetDouble()));
            }
            // These are exact profile identifiers from JSON, not computed measurements: no tolerance.
            if (weaponId != "L81" || !minRange.Equals(132d) || !maxRange.Equals(684d))
            {
                return (null, "CORRUPT_DATA");
            }
            return TryCreate(weaponId, minRange, maxRange, samples);
        }
        catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException or UnauthorizedAccessException)
        {
            return (null, "CORRUPT_DATA");
        }
    }
}
