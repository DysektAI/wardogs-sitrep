using System.IO;
using Sitrep.Core;

namespace Sitrep.Desktop;

public sealed class AssistantService : IDisposable
{
    private readonly AssistantState _state;
    private readonly OcrEngine _ocr;
    private readonly AppConfig _config;
    private readonly SemaphoreSlim _ocrGate = new(1, 1);
    private bool _disposed;

    public event Action? Changed;

    public AssistantService(AssistantState state, OcrEngine ocr, AppConfig config)
    {
        _state = state;
        _ocr = ocr;
        _config = config;
    }

    public AssistantState State => _state;
    public bool OcrReady => _ocr.IsReady;

    public bool IsForegroundAllowed(IntPtr hwnd)
    {
        if (_config.DesktopTestMode)
        {
            return true;
        }
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(_config.ForegroundTitleContains))
        {
            return false;
        }
        string title = Win32.GetWindowTitle(hwnd);
        return title.Contains(_config.ForegroundTitleContains, StringComparison.OrdinalIgnoreCase);
    }

    public void RequestOrigin(IntPtr hwnd, int cursorX, int cursorY, Func<CaptureRequest, System.Drawing.Bitmap?> capture)
    {
        var req = _state.BeginOrigin(hwnd.ToInt64(), cursorX, cursorY, 0, 0, 0, 0);
        Changed?.Invoke();
        _ = ProcessAsync(req, capture);
    }

    public void RequestTarget(IntPtr hwnd, int cursorX, int cursorY, Func<CaptureRequest, System.Drawing.Bitmap?> capture)
    {
        var (req, _) = _state.BeginTarget(hwnd.ToInt64(), cursorX, cursorY, 0, 0, 0, 0);
        if (req is null)
        {
            Changed?.Invoke();
            return;
        }
        Changed?.Invoke();
        _ = ProcessAsync(req, capture);
    }

    private async Task ProcessAsync(CaptureRequest req, Func<CaptureRequest, System.Drawing.Bitmap?> capture)
    {
        await _ocrGate.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }
            if (!ReferenceEquals(_state.Pending, req) && _state.Pending?.Sequence != req.Sequence)
            {
                return;
            }
            System.Drawing.Bitmap? bmp = null;
            try
            {
                bmp = capture(req);
            }
            catch
            {
                bmp = null;
            }
            if (bmp is null)
            {
                var fail = new OcrCompletion(req.Sequence, req.Generation, req.Role, req.OriginRevision, false, default, string.Empty, req.Role == CaptureRole.Origin ? "CAPTURE_FAILED" : "CAPTURE_FAILED");
                _state.Complete(fail);
                Changed?.Invoke();
                return;
            }
            using (bmp)
            {
                if (Win32.GetForegroundWindow().ToInt64() != req.ForegroundHwnd)
                {
                    var moved = new OcrCompletion(req.Sequence, req.Generation, req.Role, req.OriginRevision, false, default, string.Empty, "MOVED—TRY AGAIN");
                    _state.Complete(moved);
                    Changed?.Invoke();
                    return;
                }
                var result = await Task.Run(() => RecognitionPipeline.Recognize(bmp, _ocr));
                MaybeSaveDebug(req, bmp, result);
                var completion = new OcrCompletion(req.Sequence, req.Generation, req.Role, req.OriginRevision, result.Success, result.Coordinate, result.RawText, result.RejectionReason);
                _state.Complete(completion);
                Changed?.Invoke();
            }
        }
        finally
        {
            _ocrGate.Release();
        }
    }

    private void MaybeSaveDebug(CaptureRequest req, System.Drawing.Bitmap bmp, RecognitionResult result)
    {
        if (!_config.DebugMode)
        {
            return;
        }
        try
        {
            string dir = Path.Combine(AppConfig.ConfigDir, "captures");
            Directory.CreateDirectory(dir);
            var files = new DirectoryInfo(dir).GetFiles("*.png").OrderBy(f => f.CreationTimeUtc).ToList();
            while (files.Count >= 50)
            {
                files[0].Delete();
                files.RemoveAt(0);
            }
            string name = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}_{req.Role}_{req.Sequence}.png";
            bmp.Save(Path.Combine(dir, name), System.Drawing.Imaging.ImageFormat.Png);
            string rec = $"{req.Sequence} {req.Role} cursor={req.CursorX},{req.CursorY} raw={result.RawText.Replace("\n", "\\n")} reason={result.RejectionReason}";
            File.AppendAllText(Path.Combine(dir, "records.log"), rec + Environment.NewLine);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _ocrGate.Dispose();
    }
}
