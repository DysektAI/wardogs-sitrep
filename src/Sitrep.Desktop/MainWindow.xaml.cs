using System.Windows;
using System.Windows.Media;
using Sitrep.Core;

namespace Sitrep.Desktop;

public partial class MainWindow : Window
{
    private readonly AssistantService _service;
    private readonly InputMonitor _input;
    private readonly AppConfig _config;
    private readonly OverlayWindow _overlay;
    private readonly string _startupError;
    private readonly ForegroundSession _foreground;

    public MainWindow(AssistantService service, InputMonitor input, AppConfig config, OverlayWindow overlay, string startupError)
    {
        _service = service;
        _input = input;
        _config = config;
        _overlay = overlay;
        _startupError = startupError;
        _foreground = new ForegroundSession(service.State);
        InitializeComponent();
        _service.Changed += OnServiceChanged;
        _input.F8Pressed += OnF8;
        _input.F7Pressed += OnF7;
        _input.F9Pressed += OnClear;
        _input.F10Pressed += OnToggle;
        _input.MiddleClicked += OnMiddle;
        _input.Poll += ObserveForeground;
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
        var hwnd = new IntPtr(req.ForegroundHwnd);
        if (Win32.GetForegroundWindow() != hwnd || !_service.IsForegroundAllowed(hwnd))
        {
            return null;
        }
        var roi = new CaptureRegion(req.RoiX, req.RoiY, req.RoiWidth, req.RoiHeight);
        var bounds = Win32.GetCaptureBounds(hwnd, req.CursorX, req.CursorY);
        var excludes = GetVisibleExclusions();
        return ScreenCapture.CaptureRegion(roi, bounds, excludes);
    }

    internal static CaptureRegion[] GetVisibleExclusions() =>
        Application.Current?.Windows.Cast<Window>()
            .Where(w => w.IsVisible)
            .Select(w => Win32.GetScreenRegion(new System.Windows.Interop.WindowInteropHelper(w).Handle))
            .ToArray() ?? [];

    private void GuardedCapture(CaptureRole role)
    {
        if (!_service.State.LiveEnabled || !TryAnchor(out var hwnd, out int cx, out int cy))
        {
            return;
        }
        ObserveForeground();
        if (!_foreground.IsForeground)
        {
            return;
        }
        _service.Request(role, hwnd, cx, cy, CaptureFor);
    }

    private IntPtr _lastForeground;

    private void ObserveForeground()
    {
        var hwnd = Win32.GetForegroundWindow();
        if (hwnd != _lastForeground)
        {
            _lastForeground = hwnd;
            _input.ResetEdges();
        }
        if (_foreground.Observe(hwnd.ToInt64(), _service.IsForegroundAllowed(hwnd), h => Win32.IsWindow(new IntPtr(h))))
        {
            _service.DiscardPending();
            Refresh();
        }
    }

    private void OnF8() => GuardedCapture(CaptureRole.Origin);
    private void OnF7() => GuardedCapture(CaptureRole.Target);
    private void OnMiddle() => GuardedCapture(CaptureRole.Target);
    private void OnClear()
    {
        _service.State.Clear();
        _service.DiscardPending();
        Refresh();
    }

    private void OnToggle()
    {
        if (!string.IsNullOrWhiteSpace(_startupError) || OwnedWindows.OfType<SettingsWindow>().Any(w => w.IsVisible))
        {
            return;
        }
        _service.State.SetLiveEnabled(!_service.State.LiveEnabled);
        _service.DiscardPending();
        _input.SetEnabled(_service.State.LiveEnabled);
        ObserveForeground();
        Refresh();
    }

    private void EnableButton_Click(object sender, RoutedEventArgs e) => OnToggle();
    private void ClearButton_Click(object sender, RoutedEventArgs e) => OnClear();

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _service.State.SetLiveEnabled(false);
        _service.DiscardPending();
        _input.SetEnabled(false);
        Refresh();
        var settings = new SettingsWindow(_config, _overlay) { Owner = this };
        if (settings.ShowDialog().GetValueOrDefault())
        {
            _foreground.Reset();
        }
        ObserveForeground();
        Refresh();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private static Brush ThemeBrush(string key) =>
        Application.Current?.TryFindResource(key) as Brush ?? Brushes.LightGray;

    private void Refresh()
    {
        var s = _service.State;
        // While live capture is off the hotkeys are inert and no solution can exist,
        // so every surface reports DISABLED regardless of the last machine status.
        string status = s.LiveEnabled ? s.Status : DisplayStatuses.Disabled;
        StatusLevel level = StatusLevelMapper.ForStatus(status);

        StatusText.Text = status;
        StatusDot.Fill = StatusLevelMapper.BrushFor(level);

        var liveLevel = s.LiveEnabled ? StatusLevel.Success : StatusLevel.Neutral;
        LivePillText.Text = s.LiveEnabled ? "LIVE ON" : "LIVE OFF";
        LivePillText.Foreground = ThemeBrush(s.LiveEnabled ? "TextBrush" : "TextDimBrush");
        LiveDot.Fill = StatusLevelMapper.BrushFor(liveLevel);
        LivePill.BorderBrush = StatusLevelMapper.BrushFor(liveLevel);

        bool hasStartupError = !string.IsNullOrWhiteSpace(_startupError);
        StartupBanner.Visibility = hasStartupError ? Visibility.Visible : Visibility.Collapsed;
        if (hasStartupError)
        {
            StartupBannerText.Text = _startupError;
        }

        bool tableOk = s.Table is not null;
        WeaponText.Text = tableOk ? "L81 · table-backed" : "L81 · TABLE UNVERIFIED";
        WeaponText.Foreground = tableOk ? ThemeBrush("TextDimBrush") : StatusLevelMapper.BrushFor(StatusLevel.Warning);

        ElevationValue.Text = s.ElevationMil.HasValue ? $"{s.ElevationMil.Value:0.0}" : "—";
        ElevationValue.Foreground = level == StatusLevel.Success ? StatusLevelMapper.BrushFor(StatusLevel.Success) : ThemeBrush("TextBrush");
        RangeValue.Text = s.RangeMeters.HasValue ? $"{s.RangeMeters.Value:0.0} m" : "—";
        BearingValue.Text = s.BearingDegrees.HasValue ? GeoMath.FormatBearing(s.BearingDegrees.Value) : "—";
        OriginValue.Text = s.ConfirmedOrigin.HasValue ? $"x {s.ConfirmedOrigin.Value.X:0.00}  y {s.ConfirmedOrigin.Value.Y:0.00}" : "—";
        TargetValue.Text = s.ActiveTarget.HasValue ? $"x {s.ActiveTarget.Value.X:0.00}  y {s.ActiveTarget.Value.Y:0.00}" : "—";

        EnableButton.Content = s.LiveEnabled ? "Disable live" : "Enable live";
        ClearButton.IsEnabled = s.ConfirmedOrigin.HasValue || s.ActiveTarget.HasValue || s.Pending is not null;

        EnableButton.IsEnabled = !hasStartupError;
        if (s.LiveEnabled && !_foreground.IsForeground)
        {
            _overlay.Hide();
            return;
        }
        string overlay = $"L81 · uncorrected table\n{status}";
        if (s.RangeMeters.HasValue && s.BearingDegrees.HasValue)
        {
            overlay += $" | {s.RangeMeters.Value:0.0}m {GeoMath.FormatBearing(s.BearingDegrees.Value)}";
        }
        if (s.ElevationMil.HasValue)
        {
            overlay += $" | {s.ElevationMil.Value:0.0} MIL";
        }
        overlay += $"\nOrigin {OriginValue.Text}\nTarget {TargetValue.Text}";
        _overlay.ShowText(overlay, level);
    }
}

