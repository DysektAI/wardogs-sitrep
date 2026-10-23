using System.Windows;
using System.Windows.Media;
using Sitrep.Core;

namespace Sitrep.Desktop;

public static class StatusLevelMapper
{
    public static string ResourceKeyFor(StatusLevel level) => level switch
    {
        StatusLevel.Success => "SuccessBrush",
        StatusLevel.Working => "WorkingBrush",
        StatusLevel.Warning => "WarningBrush",
        StatusLevel.Error => "ErrorBrush",
        StatusLevel.Neutral => "DisabledBrush",
        _ => "InfoBrush",
    };

    public static Brush BrushFor(StatusLevel level) =>
        Application.Current?.TryFindResource(ResourceKeyFor(level)) as Brush ?? Brushes.LightGray;
}
