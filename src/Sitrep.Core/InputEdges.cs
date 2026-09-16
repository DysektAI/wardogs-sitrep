namespace Sitrep.Core;

/// <summary>High-bit input sampling; seed at enable/focus transitions to avoid phantom presses.</summary>
public sealed class InputEdges
{
    private readonly Dictionary<int, bool> _previous = new();

    public void Seed(int key, bool down) => _previous[key] = down;

    public bool Pressed(int key, bool down, bool enabled)
    {
        bool wasDown = _previous.GetValueOrDefault(key);
        _previous[key] = down;
        return enabled && down && !wasDown;
    }
}
