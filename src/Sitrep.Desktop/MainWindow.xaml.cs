using System.Windows;
using Sitrep.Core;

namespace Sitrep.Desktop;

public partial class MainWindow : Window
{
    private readonly AssistantService _service;
    private readonly InputMonitor _input;
    private readonly AppConfig _config;
    private readonly OverlayWindow _overlay;
    private readonly string _startupError;

    public MainWindow(AssistantService service, InputMonitor input, AppConfig config, OverlayWindow overlay, string startupError)
    {
        _service = service;
        _input = input;
        _config = config;
        _overlay = overlay;
        _startupError = startupError;
        InitializeComponent();
        _service.Changed += OnServiceChanged;
        _input.F8Pressed += OnF8;
        _input.F7Pressed += OnF7;
        _input.F9Pressed += OnClear;
        _input.F10Pressed += OnToggle;
        _input.MiddleClicked += OnMiddle;
        Closed += (_, __) => Application.Current.Shutdown();
        Refresh();
    }

    private void OnServiceChanged() => Dispatcher.BeginInvoke(Refresh);

    private bool TryAnchor(out IntPtr hwnd, out int cx, out int cy)
    {
        hwnd = Win32.GetForegroundWindow();
        cx = 0;
        cy = 0;
        if (!Win32.GetCursorPos(out var pt))
        {
            return false;
        }
        cx = pt.X;
        cy = pt.Y;
        return true;
    }

    private System.Drawing.Bitmap? CaptureFor(CaptureRequest req)
    {
        if (!TryAnchor(out _, out int cx, out int cy))
        {
            return null;
        }
        var roi = ScreenCapture.BuildRoi(cx, cy, _config);
        var excludes = new List<CaptureRegion>();
        try
        {
            var r = _overlay.GetScreenRect();
            excludes.Add(new CaptureRegion(r.X, r.Y, r.Width, r.Height));
        }
        catch
        {
        }
        var frame = ScreenCapture.CaptureCursorRegion(cx, cy, roi, Win32.GetForegroundWindow(), excludes);
        if (frame is null)
        {
            return null;
        }
        return frame.Image;
    }

    private void GuardedOrigin()
    {
        if (!TryAnchor(out var hwnd, out int cx, out int cy))
        {
            return;
        }
        if (hwnd == IntPtr.Zero)
        {
            _service.State.OnGameWindowClosed();
            Refresh();
            return;
        }
        if (!_service.IsForegroundAllowed(hwnd))
        {
            _service.State.OnForegroundLost();
            Refresh();
            return;
        }
        _service.RequestOrigin(hwnd, cx, cy, CaptureFor);
    }

    private void GuardedTarget()
    {
        if (!TryAnchor(out var hwnd, out int cx, out int cy))
        {
            return;
        }
        if (hwnd == IntPtr.Zero)
        {
            _service.State.OnGameWindowClosed();
            Refresh();
            return;
        }
        if (!_service.IsForegroundAllowed(hwnd))
        {
            _service.State.OnForegroundLost();
            Refresh();
            return;
        }
        _service.RequestTarget(hwnd, cx, cy, CaptureFor);
    }

    private void OnF8() => Dispatcher.BeginInvoke(GuardedOrigin);
    private void OnF7() => Dispatcher.BeginInvoke(GuardedTarget);
    private void OnMiddle() => Dispatcher.BeginInvoke(GuardedTarget);
    private void OnClear() => Dispatcher.BeginInvoke(() => { _service.State.Clear(); Refresh(); });
    private void OnToggle() => Dispatcher.BeginInvoke(() =>
    {
        _service.State.SetLiveEnabled(!_service.State.LiveEnabled);
        _input.SetEnabled(_service.State.LiveEnabled);
        Refresh();
    });

    private void EnableButton_Click(object sender, RoutedEventArgs e) => OnToggle();
    private void ClearButton_Click(object sender, RoutedEventArgs e) => OnClear();
    private void ExitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void Refresh()
    {
        var s = _service.State;
        string status = s.Status;
        if (!string.IsNullOrWhiteSpace(_startupError))
        {
            status += $" | STARTUP: {_startupError}";
        }
        StatusText.Text = status;
        WeaponText.Text = s.Table is null ? "Weapon: L81 (TABLE UNVERIFIED)" : "Weapon: L81 (table-backed)";
        OriginText.Text = s.ConfirmedOrigin.HasValue ? $"Origin: x{s.ConfirmedOrigin.Value.X:0.00} y{s.ConfirmedOrigin.Value.Y:0.00}" : "Origin: —";
        TargetText.Text = s.ActiveTarget.HasValue ? $"Target: x{s.ActiveTarget.Value.X:0.00} y{s.ActiveTarget.Value.Y:0.00}" : "Target: —";
        RangeText.Text = s.RangeMeters.HasValue ? $"Range: {s.RangeMeters.Value:0.0} m" : "Range: —";
        BearingText.Text = s.BearingDegrees.HasValue ? $"Bearing: {s.BearingDegrees.Value:0.0}°" : "Bearing: —";
        ElevationText.Text = s.ElevationMil.HasValue ? $"Elevation: {s.ElevationMil.Value:0.0} MIL (uncorrected table)" : "Elevation: —";
        EnableButton.Content = s.LiveEnabled ? "Disable" : "Enable";
        string overlay = s.Status;
        if (s.RangeMeters.HasValue && s.BearingDegrees.HasValue)
        {
            overlay += $" | {s.RangeMeters.Value:0.0}m {s.BearingDegrees.Value:0.0}°";
        }
        if (s.ElevationMil.HasValue)
        {
            overlay += $" | {s.ElevationMil.Value:0.0} MIL";
        }
        _overlay.ShowText(overlay);
    }
}
