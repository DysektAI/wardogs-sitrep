namespace Sitrep.Core;

public static class RecognitionConsensus
{
    public static bool TryAccept(IEnumerable<string> recipes, out MapCoordinate coordinate, out string reason)
    {
        coordinate = default;
        reason = "NO_COORDINATES";
        MapCoordinate? accepted = null;
        foreach (string raw in recipes)
        {
            if (!CoordinateParser.TryParse(raw, out var candidate, out var rejection))
            {
                if (rejection == "CONFLICTING_PAIR")
                {
                    reason = "CONFLICTING_RECIPES";
                    return false;
                }
                reason = rejection;
                continue;
            }
            if (accepted.HasValue && candidate != accepted.Value)
            {
                reason = "CONFLICTING_RECIPES";
                return false;
            }
            accepted = candidate;
        }
        if (!accepted.HasValue)
        {
            return false;
        }
        coordinate = accepted.Value;
        reason = string.Empty;
        return true;
    }
}
