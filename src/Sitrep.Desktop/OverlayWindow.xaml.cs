using System.Windows;
using System.Windows.Interop;

namespace Sitrep.Desktop;

public partial class OverlayWindow : Window
{
    public OverlayWindow(AppConfig config)
    {
        InitializeComponent();
        if (config.OverlayLeft >= 0 && config.OverlayTop >= 0)
        {
            Left = config.OverlayLeft;
            Top = config.OverlayTop;
        }
        else
        {
            Left = Math.Max(0, SystemParameters.PrimaryScreenWidth - Width - 24);
            Top = 64;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
        Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, ex | Win32.WS_EX_TRANSPARENT | Win32.WS_EX_LAYERED | Win32.WS_EX_NOACTIVATE);
        IsHitTestVisible = false;
    }

    public void ShowText(string text)
    {
        OverlayText.Text = text;
        if (!IsVisible)
        {
            Show();
        }
    }

    public System.Drawing.Rectangle GetScreenRect()
    {
        var p1 = PointToScreen(new Point(0, 0));
        var p2 = PointToScreen(new Point(ActualWidth, ActualHeight));
        double scaleX = 1.0, scaleY = 1.0;
        try
        {
            var src = PresentationSource.FromVisual(this);
            if (src?.CompositionTarget != null)
            {
                scaleX = src.CompositionTarget.TransformToDevice.M11;
                scaleY = src.CompositionTarget.TransformToDevice.M22;
            }
        }
        catch
        {
        }
        int x = (int)(p1.X * scaleX);
        int y = (int)(p1.Y * scaleX);
        int w = Math.Max(1, (int)((p2.X - p1.X) * scaleX));
        int h = Math.Max(1, (int)((p2.Y - p1.Y) * scaleY));
        return new System.Drawing.Rectangle(x, y, w, h);
    }
}
