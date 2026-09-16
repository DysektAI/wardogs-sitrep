namespace WardogsMortar.Desktop;

public sealed class InputMonitor : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly Dictionary<int, bool> _prev = new();
    private bool _enabled;
    private bool _disposed;
    private long _missedHeartbeats;

    public event Action? F8Pressed;
    public event Action? F7Pressed;
    public event Action? F9Pressed;
    public event Action? F10Pressed;
    public event Action? MiddleClicked;

    public InputMonitor()
    {
        _timer = new System.Threading.Timer(Tick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        lock (_prev)
        {
            _prev.Clear();
            foreach (int vk in new[] { Win32.VK_F7, Win32.VK_F8, Win32.VK_F9, Win32.VK_F10, Win32.VK_MBUTTON })
            {
                _prev[vk] = IsDown(vk);
            }
        }
        if (enabled)
        {
            _timer.Change(0, 10);
        }
        else
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private static bool IsDown(int vk) => (Win32.GetAsyncKeyState(vk) & 0x8000) != 0;

    private void Tick(object? _)
    {
        if (!_enabled || _disposed)
        {
            return;
        }
        try
        {
            Check(Win32.VK_F8, F8Pressed);
            Check(Win32.VK_F7, F7Pressed);
            Check(Win32.VK_F9, F9Pressed);
            Check(Win32.VK_F10, F10Pressed);
            Check(Win32.VK_MBUTTON, MiddleClicked);
        }
        catch
        {
            Interlocked.Increment(ref _missedHeartbeats);
        }
    }

    private void Check(int vk, Action? handler)
    {
        bool down = IsDown(vk);
        bool was;
        lock (_prev)
        {
            was = _prev.TryGetValue(vk, out bool v) && v;
            _prev[vk] = down;
        }
        if (down && !was)
        {
            handler?.Invoke();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Dispose();
    }
}
