using Sitrep.Core;

namespace Sitrep.Tests;

public sealed class StatusLevelTests
{
    public static TheoryData<string, StatusLevel> Statuses => new()
    {
        { DisplayStatuses.Ready, StatusLevel.Success },
        { DisplayStatuses.Reading, StatusLevel.Working },
        { DisplayStatuses.WindowLost, StatusLevel.Warning },
        { DisplayStatuses.Moved, StatusLevel.Warning },
        { DisplayStatuses.OutsideMap, StatusLevel.Warning },
        { DisplayStatuses.Disabled, StatusLevel.Neutral },
        { DisplayStatuses.OutOfRange, StatusLevel.Error },
        { DisplayStatuses.TableUnavailable, StatusLevel.Error },
        { DisplayStatuses.TargetFailed(""), StatusLevel.Error },
        { DisplayStatuses.TargetFailed("NO_COORDINATES"), StatusLevel.Error },
        { DisplayStatuses.OriginFailed(""), StatusLevel.Error },
        { DisplayStatuses.OriginFailed("BAD_X_PRECISION"), StatusLevel.Error },
        { DisplayStatuses.SetMortar, StatusLevel.Info },
        { DisplayStatuses.OriginSet, StatusLevel.Info },
    };

    [Theory]
    [MemberData(nameof(Statuses))]
    public void MapsEveryDisplayStatusToItsSeverity(string status, StatusLevel expected) =>
        Assert.Equal(expected, StatusLevels.ForStatus(status));
}
