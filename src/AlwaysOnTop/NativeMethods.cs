using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AlwaysOnTop;

/// <summary>
/// P/Invoke declarations for the Win32 APIs used to register a global hotkey,
/// query the foreground window and toggle its top-most (Always On Top) state.
/// </summary>
internal static class NativeMethods
{
    // ---- Global hotkey ----------------------------------------------------
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public const int WM_HOTKEY = 0x0312;

    // ---- Foreground / window rectangle ------------------------------------
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>True when the window is minimized (iconic).</summary>
    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    /// <summary>Owning process id of a window - used to detect handle recycling.</summary>
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>
    /// Read a window's extended style flags. Used to check whether a pinned
    /// window still has WS_EX_TOPMOST so it can be re-asserted if it was lost.
    /// (GetWindowLongW works on x64 for indices that fit in 32 bits, like
    /// GWL_EXSTYLE.)
    /// </summary>
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongW")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOPMOST = 0x00000008;

    // ---- DPI / DWM: the *visible* frame bounds for a snug border ------------
    // GetWindowRect includes the invisible resize borders DWM adds, so a border
    // drawn from it looks loose. DWMWA_EXTENDED_FRAME_BOUNDS returns the real
    // visible rectangle, which lets the overlay hug the window exactly.
    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute,
        out RECT pvAttribute, int cbAttribute);

    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    /// <summary>Per-monitor DPI for a window (Win10 1607+); used to size rounded corners.</summary>
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hWnd);

    // ---- SetWindowPos (top-most toggle & border positioning) --------------
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    // ---- Extended window styles for the click-through border overlay ------
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr h);

    // ---- Region-shaped border overlay (rounded ring) -----------------------
    // The overlay is clipped to a rounded "ring" via SetWindowRgn. This renders
    // reliably (no layered/bitmap quirks) and the interior is a true hole, so
    // the pinned window shows through and is never covered.
    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2,
        int nWidthEllipse, int nHeightEllipse);

    [DllImport("gdi32.dll")]
    public static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1,
        IntPtr hrgnSrc2, int fnCombineMode);

    [DllImport("user32.dll")]
    public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    public const int RGN_DIFF = 4;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }
}
