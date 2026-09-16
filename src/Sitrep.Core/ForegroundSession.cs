namespace Sitrep.Core;

/// <summary>Binds confirmed positions to one window; foreground changes never transfer its origin.</summary>
public sealed class ForegroundSession(AssistantState state)
{
    public long AttachedWindow { get; private set; }
    public bool IsForeground { get; private set; }

    public bool Observe(long foreground, bool allowed, Func<long, bool> exists)
    {
        bool changed = false;
        if (AttachedWindow != 0 && !exists(AttachedWindow))
        {
            AttachedWindow = 0;
            state.OnGameWindowClosed();
            changed = true;
        }
        if (AttachedWindow == 0 && allowed && state.LiveEnabled)
        {
            AttachedWindow = foreground;
        }
        bool active = allowed && foreground != 0 && foreground == AttachedWindow;
        if (IsForeground && !active)
        {
            // Closure already cleared all state; preserve its SET MORTAR message.
            if (!changed) { state.OnForegroundLost(); }
        }
        changed |= active != IsForeground;
        IsForeground = active;
        return changed;
    }

    public void Reset()
    {
        AttachedWindow = 0;
        IsForeground = false;
        state.Clear();
    }
}
