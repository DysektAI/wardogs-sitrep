using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Sitrep.Desktop;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly AppConfig _draft;
    private readonly OverlayWindow _overlay;

    public SettingsWindow(AppConfig config, OverlayWindow overlay)
    {
        _config = config;
        _overlay = overlay;
        // Edit a working copy; the live config (and disk) change only on a successful Save.
        _draft = new AppConfig
        {
            ForegroundTitleContains = config.ForegroundTitleContains,
            DesktopTestMode = config.DesktopTestMode,
            RoiWidth = config.RoiWidth,
            RoiHeight = config.RoiHeight,
            RoiOffsetX = config.RoiOffsetX,
            RoiOffsetTop = config.RoiOffsetTop,
            DebugMode = config.DebugMode,
            TessDataDir = config.TessDataDir,
            OverlayLeft = config.OverlayLeft,
            OverlayTop = config.OverlayTop,
        };
        InitializeComponent();
        LoadFromDraft();
        DesktopTestBox_Changed(this, new RoutedEventArgs());
        UpdateOverlayPositionText();
    }

    private void LoadFromDraft()
    {
        TitleContainsBox.Text = _draft.ForegroundTitleContains;
        DesktopTestBox.IsChecked = _draft.DesktopTestMode;
        RoiWidthBox.Text = _draft.RoiWidth.ToString(CultureInfo.InvariantCulture);
        RoiHeightBox.Text = _draft.RoiHeight.ToString(CultureInfo.InvariantCulture);
        RoiOffsetXBox.Text = _draft.RoiOffsetX.ToString(CultureInfo.InvariantCulture);
        RoiOffsetTopBox.Text = _draft.RoiOffsetTop.ToString(CultureInfo.InvariantCulture);
        DebugModeBox.IsChecked = _draft.DebugMode;
    }

    private void UpdateOverlayPositionText()
    {
        OverlayPosText.Text = _draft.OverlayLeft >= 0 && _draft.OverlayTop >= 0
            ? $"Custom ({_draft.OverlayLeft:0}, {_draft.OverlayTop:0})"
            : "Default (top-right)";
    }

    private void DesktopTestBox_Changed(object sender, RoutedEventArgs e)
    {
        TitleContainsBox.IsEnabled = DesktopTestBox.IsChecked != true;
    }

    private void ResetOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        // Preview on the draft; reverted from the live config if the dialog is cancelled.
        _draft.OverlayLeft = -1;
        _draft.OverlayTop = -1;
        _overlay.ApplyPosition(_draft);
        UpdateOverlayPositionText();
    }

    private static bool TryReadInt(TextBox box, int min, int max, out int value)
    {
        if (int.TryParse(box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            && value >= min && value <= max)
        {
            return true;
        }
        value = 0;
        return false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadInt(RoiWidthBox, 40, 2000, out int width)
            || !TryReadInt(RoiHeightBox, 40, 2000, out int height)
            || !TryReadInt(RoiOffsetXBox, -2000, 2000, out int offsetX)
            || !TryReadInt(RoiOffsetTopBox, -2000, 2000, out int offsetTop))
        {
            ValidationText.Text = "Capture region values must be whole numbers: width/height 40 to 2000 px, offsets -2000 to 2000 px.";
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        _draft.ForegroundTitleContains = TitleContainsBox.Text.Trim();
        _draft.DesktopTestMode = DesktopTestBox.IsChecked == true;
        _draft.RoiWidth = width;
        _draft.RoiHeight = height;
        _draft.RoiOffsetX = offsetX;
        _draft.RoiOffsetTop = offsetTop;
        _draft.DebugMode = DebugModeBox.IsChecked == true;

        try
        {
            _draft.Save();
        }
        catch (Exception ex)
        {
            ValidationText.Text = $"Could not save config.json: {ex.Message}";
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        _config.ForegroundTitleContains = _draft.ForegroundTitleContains;
        _config.DesktopTestMode = _draft.DesktopTestMode;
        _config.RoiWidth = _draft.RoiWidth;
        _config.RoiHeight = _draft.RoiHeight;
        _config.RoiOffsetX = _draft.RoiOffsetX;
        _config.RoiOffsetTop = _draft.RoiOffsetTop;
        _config.DebugMode = _draft.DebugMode;
        _config.TessDataDir = _draft.TessDataDir;
        _config.OverlayLeft = _draft.OverlayLeft;
        _config.OverlayTop = _draft.OverlayTop;
        DialogResult = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        // X, Alt+F4, Escape and Cancel must all roll back the preview.
        if (DialogResult != true)
        {
            _overlay.ApplyPosition(_config);
        }
        base.OnClosed(e);
    }
}

