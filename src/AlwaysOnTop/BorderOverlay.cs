using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// A frameless, click-through, top-most overlay that draws a colored border
/// hugging a pinned window - the visual cue PowerToys shows around pinned
/// windows.
///
/// The window is a plain solid-color form clipped by <c>SetWindowRgn</c> into a
/// rounded "ring":
///   * The interior is a genuine hole (not part of the window), so the pinned
///     window always shows through, is never covered, and dragging other windows
///     over it leaves no artifacts.
///   * The corners are rounded to match Windows 11 windows.
///   * It positions itself from <c>DWMWA_EXTENDED_FRAME_BOUNDS</c> (the visible
///     frame) so the border hugs the window exactly.
/// This renders reliably without any layered-window / bitmap complexity.
/// </summary>
public sealed class BorderOverlay : Form
{
    private readonly IntPtr _target;
    private readonly int _thickness;

    private int _lastX = int.MinValue, _lastY = int.MinValue;
    private int _lastW = -1, _lastH = -1;

    public BorderOverlay(IntPtr target, Color borderColor, int thickness)
    {
        _target = target;
        _thickness = Math.Max(1, thickness);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Enabled = false;                    // never take focus
        AutoScaleMode = AutoScaleMode.None; // work in raw device pixels
        BackColor = borderColor;            // clipped to the ring by SetWindowRgn
        DoubleBuffered = true;
        Size = new Size(1, 1);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TRANSPARENT   // click-through
                        | NativeMethods.WS_EX_NOACTIVATE
                        | NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    // Do not activate / steal focus when shown.
    protected override bool ShowWithoutActivation => true;

    /// <summary>
    /// Re-align the overlay to the current target window rectangle. Hides itself
    /// while the target is minimized or hidden, so no stray border is left behind.
    /// </summary>
    public void UpdatePosition()
    {
        if (!NativeMethods.IsWindow(_target) ||
            NativeMethods.IsIconic(_target) ||
            !NativeMethods.IsWindowVisible(_target) ||
            !TryGetVisibleRect(_target, out NativeMethods.RECT r))
        {
            HideOverlay();
            return;
        }

        int t = _thickness;
        int x = r.Left - t;
        int y = r.Top - t;
        int w = r.Width + (2 * t);
        int h = r.Height + (2 * t);
        if (w <= 0 || h <= 0)
        {
            HideOverlay();
            return;
        }

        if (!Visible) Show();

        if (w != _lastW || h != _lastH)
        {
            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, x, y, w, h,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            ApplyRing(w, h, t);
        }
        else if (x != _lastX || y != _lastY)
        {
            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, x, y, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        }

        // Stay in the top-most band above newly opened windows.
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

        _lastX = x; _lastY = y; _lastW = w; _lastH = h;
    }

    private void HideOverlay()
    {
        if (Visible) Hide();
        _lastW = _lastH = -1; // force a re-shape when it reappears
    }

    /// <summary>
    /// Clip the window to a rounded ring: an outer rounded rectangle with the
    /// inner (window-sized) rounded rectangle subtracted.
    /// </summary>
    private void ApplyRing(int w, int h, int t)
    {
        int rInner = CornerRadius();
        int rOuter = rInner + t;

        IntPtr outer = NativeMethods.CreateRoundRectRgn(0, 0, w, h, 2 * rOuter, 2 * rOuter);
        IntPtr inner = NativeMethods.CreateRoundRectRgn(t, t, w - t, h - t, 2 * rInner, 2 * rInner);
        NativeMethods.CombineRgn(outer, outer, inner, NativeMethods.RGN_DIFF);

        // The system takes ownership of 'outer' and frees the previous region.
        NativeMethods.SetWindowRgn(Handle, outer, true);
        NativeMethods.DeleteObject(inner);
    }

    /// <summary>Prefer the DWM visible frame bounds; fall back to GetWindowRect.</summary>
    private static bool TryGetVisibleRect(IntPtr hwnd, out NativeMethods.RECT rect)
    {
        int hr = NativeMethods.DwmGetWindowAttribute(
            hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
            out rect, Marshal.SizeOf<NativeMethods.RECT>());

        if (hr == 0 && rect.Width > 0 && rect.Height > 0)
            return true;

        return NativeMethods.GetWindowRect(hwnd, out rect);
    }

    /// <summary>Corner radius that tracks the window's own rounded corners (~8 DIP).</summary>
    private int CornerRadius()
    {
        uint dpi = NativeMethods.GetDpiForWindow(_target);
        if (dpi == 0) dpi = 96;
        return (int)Math.Round(8.0 * dpi / 96.0);
    }
}
