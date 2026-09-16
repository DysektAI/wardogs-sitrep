using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Interop;
using Sitrep.Core;

namespace Sitrep.Desktop;

internal static class IntegrationChecks
{
    public static async Task<int> RunAsync()
    {
        try
        {
            CheckInvalidStartupConfig();
            CheckPreprocessing();
            CheckWindowBounds();
            CheckDebugRetention();
            await CheckSnapshotQueueAsync();
            await CheckRetryMovementAsync();
            await CheckCompletionGuardAsync(closed: false);
            await CheckCompletionGuardAsync(closed: true);
            Console.WriteLine("INTEGRATION OK: preprocessing parity/ownership, native overlay bounds/styles, bounded debug records, snapshot queue, OCR failure, completion focus/closure guards.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INTEGRATION FAIL: {ex}");
            return 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) { throw new TimeoutException("Integration condition did not complete."); }
            await Task.Delay(10);
        }
    }

    private static Bitmap Frame(byte marker)
    {
        var bitmap = new Bitmap(40, 40, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(marker, 100, 100));
        return bitmap;
    }

    private static void CheckInvalidStartupConfig()
    {
        string path = Path.Combine(Path.GetTempPath(), "Sitrep-config-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            foreach (string contents in new[] { "null", "{", "{\"RoiWidth\":0}" })
            {
                File.WriteAllText(path, contents);
                var errors = new List<string>();
                var config = App.LoadInteractiveConfig(errors.Add, path);
                Require(config is null && errors.Count == 1 && errors[0].Contains(path, StringComparison.Ordinal),
                    "Invalid startup config must produce one actionable error and no usable config.");
                Require(File.ReadAllText(path) == contents, "Startup overwrote invalid config.");
            }
        }
        finally { File.Delete(path); }
    }

    private static void CheckPreprocessing()
    {
        using var source = Frame(180);
        source.SetPixel(10, 10, Color.FromArgb(255, 20, 140, 220));
        foreach (bool threshold in new[] { false, true })
        {
            using var actual = ScreenCapture.PreprocessForOcr(source, threshold);
            using var scaled = new Bitmap(source.Width * 2, source.Height * 2, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, scaled.Width, scaled.Height);
            }
            using var padded = new Bitmap(scaled.Width + 20, scaled.Height + 20, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(padded))
            {
                g.Clear(Color.Black);
                g.DrawImageUnscaled(scaled, 10, 10);
            }
            for (int y = 0; y < actual.Height; y++)
            {
                for (int x = 0; x < actual.Width; x++)
                {
                    Color before = padded.GetPixel(x, y);
                    int gray = (299 * before.R + 587 * before.G + 114 * before.B) / 1000;
                    int expected = threshold ? (gray > 140 ? 255 : 0) : gray;
                    Color after = actual.GetPixel(x, y);
                    Require(after.R == expected && after.G == expected && after.B == expected && after.A == before.A,
                        "LockBits preprocessing changed pixels.");
                }
            }
            Require(source.Width == 40, "Preprocessing disposed its borrowed input.");
        }
    }

    private static void CheckWindowBounds()
    {
        var overlay = new OverlayWindow(new AppConfig());
        try
        {
            overlay.ShowText("L81 · uncorrected table\nDISABLED\nOrigin —\nTarget —", StatusLevel.Neutral);
            overlay.UpdateLayout();
            var hwnd = new WindowInteropHelper(overlay).Handle;
            var region = Win32.GetScreenRegion(hwnd);
            var topLeft = overlay.PointToScreen(new System.Windows.Point(0, 0));
            var bottomRight = overlay.PointToScreen(new System.Windows.Point(overlay.ActualWidth, overlay.ActualHeight));
            Require(Math.Abs(region.X - topLeft.X) <= 1 && Math.Abs(region.Y - topLeft.Y) <= 1
                && Math.Abs(region.Width - (bottomRight.X - topLeft.X)) <= 1
                && Math.Abs(region.Height - (bottomRight.Y - topLeft.Y)) <= 1,
                "Native exclusion bounds do not match WPF physical pixels.");
            long style = Win32.GetExtendedStyle(hwnd);
            long mask = Win32.WS_EX_TRANSPARENT | Win32.WS_EX_LAYERED | Win32.WS_EX_NOACTIVATE;
            Require((style & mask) == mask, "Missing click-through/non-activation styles.");
            Require(ScreenCapture.CaptureRegion(region, region, [region]) is null, "Own-window overlap accepted.");
            Console.WriteLine($"Overlay bounds/styles checked at DPI {System.Windows.Media.VisualTreeHelper.GetDpi(overlay).PixelsPerInchX}.");
        }
        finally { overlay.Close(); }
    }

    private static void CheckDebugRetention()
    {
        string dir = Path.Combine(Path.GetTempPath(), "Sitrep-integration-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "records.log"), "legacy log");
            using var frame = Frame(100);
            for (int i = 0; i < 60; i++)
            {
                var request = new CaptureRequest(i, 0, CaptureRole.Target, 0, 1, DateTimeOffset.UtcNow, 10, 20, 30, 40, 40, 40);
                DebugCaptureStore.Save(request, frame, new RecognitionResult(false, default, "noise", 0, "NO_COORDINATES"), dir);
            }
            Require(Directory.GetFiles(dir).Length == 50, "Debug retention must bound images and JSON to 50 total files.");
            Require(!File.Exists(Path.Combine(dir, "records.log")), "Legacy unbounded log survived.");
        }
        finally { if (Directory.Exists(dir)) { Directory.Delete(dir, recursive: true); } }
    }

    private static async Task CheckSnapshotQueueAsync()
    {
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var state = new AssistantState(null, true);
        using var service = new AssistantService(state, new AppConfig(), image =>
        {
            byte marker = image.GetPixel(0, 0).R;
            if (marker == 1)
            {
                started.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException(); }
            }
            if (marker == 4) { throw new InvalidOperationException("synthetic native OCR failure"); }
            return new RecognitionResult(true, new MapCoordinate(marker, 100), "fixture", 1, "");
        }, () => new IntPtr(1), _ => true, _ => true, () => (400, 500));
        int captures = 0;
        var markers = new Dictionary<long, byte>();
        Bitmap Capture(CaptureRequest req)
        {
            Require(req.CursorX == 400 && req.CursorY == 500 && req.RoiX == 360 && req.RoiY == 324
                && req.RoiWidth == 360 && req.RoiHeight == 200, "Request lost its event anchor/ROI.");
            captures++;
            if (!markers.TryGetValue(req.Sequence, out byte marker))
            {
                marker = (byte)(markers.Count + 1);
                markers.Add(req.Sequence, marker);
            }
            return Frame(marker);
        }
        try
        {
            service.Request(CaptureRole.Origin, new IntPtr(1), 400, 500, Capture);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var origin = state.Pending;
            service.Request(CaptureRole.Target, new IntPtr(1), 400, 500, Capture);
            Require(captures == 2 && state.Pending == origin, "Target superseded pending origin.");
            service.Request(CaptureRole.Origin, new IntPtr(1), 400, 500, Capture);
            service.Request(CaptureRole.Origin, new IntPtr(1), 400, 500, Capture);
            Require(captures == 4, "Snapshots waited behind busy OCR instead of capturing on trigger.");
            release.Set();
            await UntilAsync(() => state.Pending is null);
            Require(state.ConfirmedOrigin == new MapCoordinate(3, 100), "Newest snapshot did not win.");
            service.Request(CaptureRole.Target, new IntPtr(1), 400, 500, Capture);
            await UntilAsync(() => state.Pending is null);
            Require(state.Status.StartsWith("TARGET OCR FAILED: OCR_ERROR", StringComparison.Ordinal)
                && state.ConfirmedOrigin.HasValue && state.ElevationMil is null, "OCR exception left stale/pending state.");
            service.Request(CaptureRole.Origin, new IntPtr(1), 400, 500, _ => throw new InvalidOperationException());
            Require(state.Pending is null && state.ConfirmedOrigin is null, "Capture exception left usable origin.");
        }
        finally { release.Set(); }
    }

    private static async Task CheckRetryMovementAsync()
    {
        var state = new AssistantState(null, true);
        int recognitions = 0;
        using var service = new AssistantService(state, new AppConfig(), _ =>
        {
            Interlocked.Increment(ref recognitions);
            return new RecognitionResult(true, new MapCoordinate(100, 100), "fixture", 1, "");
        }, () => new IntPtr(1), _ => true, _ => true, () => (401, 500));
        int captures = 0;
        service.Request(CaptureRole.Origin, new IntPtr(1), 400, 500, _ => { captures++; return Frame(1); });
        await UntilAsync(() => state.Pending is null);
        Require(captures == 1 && recognitions == 0 && state.Status == DisplayStatuses.Moved
            && state.ConfirmedOrigin is null, "Retry failed to reject a moved anchor before capture/OCR.");
    }

    private static async Task CheckCompletionGuardAsync(bool closed)
    {
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var state = new AssistantState(null, true);
        IntPtr foreground = new(1);
        bool exists = true;
        using var service = new AssistantService(state, new AppConfig(), _ =>
        {
            started.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException(); }
            return new RecognitionResult(true, new MapCoordinate(100, 100), "fixture", 1, "");
        }, () => foreground, _ => exists, _ => true, () => (400, 500));
        try
        {
            service.Request(CaptureRole.Origin, new IntPtr(1), 400, 500, _ => Frame(1));
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (closed) { exists = false; } else { foreground = new IntPtr(2); }
            release.Set();
            await UntilAsync(() => state.Pending is null);
            Require(state.ConfirmedOrigin is null && state.ElevationMil is null, "Late completion bypassed window identity guard.");
            Require(state.Status == (closed ? DisplayStatuses.SetMortar : DisplayStatuses.WindowLost), "Wrong foreground failure status.");
        }
        finally { release.Set(); }
    }
}
