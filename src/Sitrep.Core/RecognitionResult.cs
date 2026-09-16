namespace Sitrep.Core;

public sealed record RecognitionResult(bool Success, MapCoordinate Coordinate, string RawText, float Confidence, string RejectionReason)
{
    public static RecognitionResult Reconcile(RecognitionResult first, RecognitionResult retry)
    {
        string raw = first.RawText + "\n--- retry snapshot ---\n" + retry.RawText;
        if (first.RejectionReason == "CONFLICTING_RECIPES" || retry.RejectionReason == "CONFLICTING_RECIPES"
            || (first.Success && retry.Success && first.Coordinate != retry.Coordinate))
        {
            return new RecognitionResult(false, default, raw, 0, "CONFLICTING_SNAPSHOTS_OR_RECIPES");
        }
        // Never combine partial axes: each result was parsed independently from one frame.
        var result = retry.Success ? retry : first.Success ? first : retry;
        return result with { RawText = raw };
    }
}
