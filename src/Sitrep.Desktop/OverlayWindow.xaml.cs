using System.Windows;
using System.Windows.Interop;

namespace Sitrep.Desktop;

public partial class OverlayWindow : Window
{
    public OverlayWindow(AppConfig config)
    {
        InitializeComponent();
        ApplyPosition(config);
    }

    public void ApplyPosition(AppConfig config)
    {
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
        Win32.MakeClickThrough(hwnd);
    }

    public void ShowText(string text, StatusLevel level)
    {
        OverlayText.Text = text;
        AccentBar.Background = StatusLevelMapper.BrushFor(level);
        if (!IsVisible)
        {
            Show();
        }
    }

}
