using System.Windows.Threading;
using Sitrep.Core;

namespace Sitrep.Desktop;

public sealed class InputMonitor : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly InputEdges _edges = new();
    private bool _enabled;

    public event Action? F8Pressed;
    public event Action? F7Pressed;
    public event Action? F9Pressed;
    public event Action? F10Pressed;
    public event Action? MiddleClicked;
    public event Action? Poll;

    public InputMonitor()
    {
        ResetEdges();
        _timer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(10) };
        _timer.Tick += Tick;
        _timer.Start();
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        ResetEdges();
    }

    public void ResetEdges()
    {
        foreach (int vk in new[] { Win32.VK_F7, Win32.VK_F8, Win32.VK_F9, Win32.VK_F10, Win32.VK_MBUTTON })
        {
            _edges.Seed(vk, IsDown(vk));
        }
    }

    private static bool IsDown(int vk) => (Win32.GetAsyncKeyState(vk) & 0x8000) != 0;

    private void Tick(object? sender, EventArgs e)
    {
        Poll?.Invoke();
        Check(Win32.VK_F10, F10Pressed, true);
        Check(Win32.VK_F9, F9Pressed, _enabled);
        Check(Win32.VK_F8, F8Pressed, _enabled);
        Check(Win32.VK_F7, F7Pressed, _enabled);
        Check(Win32.VK_MBUTTON, MiddleClicked, _enabled);
    }

    private void Check(int vk, Action? handler, bool enabled)
    {
        if (_edges.Pressed(vk, IsDown(vk), enabled))
        {
            handler?.Invoke();
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= Tick;
    }
}
