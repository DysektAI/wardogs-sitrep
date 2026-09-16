using System.Runtime.InteropServices;

namespace Sitrep.Desktop;

internal static class Win32
{
    public const int VK_F7 = 0x76;
    public const int VK_F8 = 0x77;
    public const int VK_F9 = 0x78;
    public const int VK_F10 = 0x79;
    public const int VK_MBUTTON = 0x04;

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOPMOST = 0x00000008;

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    public static bool IsOwnWindow(IntPtr hwnd)
    {
        GetWindowThreadProcessId(hwnd, out uint processId);
        return processId == Environment.ProcessId;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
    }

    public static long GetExtendedStyle(IntPtr hwnd)
    {
        Marshal.SetLastPInvokeError(0);
        long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        int error = Marshal.GetLastPInvokeError();
        if (style == 0 && error != 0)
        {
            throw new System.ComponentModel.Win32Exception(error);
        }
        return style;
    }

    public static void MakeClickThrough(IntPtr hwnd)
    {
        long style = GetExtendedStyle(hwnd) | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE;
        Marshal.SetLastPInvokeError(0);
        var previous = SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(style));
        int error = Marshal.GetLastPInvokeError();
        if (previous == IntPtr.Zero && error != 0)
        {
            throw new System.ComponentModel.Win32Exception(error);
        }
    }

    public static Core.CaptureRegion GetScreenRegion(IntPtr hwnd)
    {
        if (!GetWindowRect(hwnd, out var rect))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        }
        return new Core.CaptureRegion(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static Core.CaptureRegion? GetCaptureBounds(IntPtr hwnd, int cursorX, int cursorY)
    {
        var origin = new POINT();
        if (!GetClientRect(hwnd, out var client) || !ClientToScreen(hwnd, ref origin))
        {
            return null;
        }
        var monitor = MonitorFromPoint(new POINT { X = cursorX, Y = cursorY }, 0);
        var info = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
        {
            return null;
        }
        return Core.RoiBuilder.Intersect(
            new Core.CaptureRegion(origin.X, origin.Y, client.Right - client.Left, client.Bottom - client.Top),
            new Core.CaptureRegion(info.Monitor.Left, info.Monitor.Top,
                info.Monitor.Right - info.Monitor.Left, info.Monitor.Bottom - info.Monitor.Top));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static string GetWindowTitle(IntPtr hwnd)
    {
        var sb = new System.Text.StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
