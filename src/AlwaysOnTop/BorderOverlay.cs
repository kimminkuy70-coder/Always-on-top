using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// A frameless, click-through, top-most overlay that draws a colored border
/// hugging a pinned window - the visual cue PowerToys shows around pinned
/// windows.
///
/// Two things make it line up with modern (Windows 11) windows:
///   * It positions itself from <c>DWMWA_EXTENDED_FRAME_BOUNDS</c> (the real
///     visible rectangle) instead of <c>GetWindowRect</c> (which includes the
///     invisible resize borders), so the frame sits snug against the window.
///   * The window is shaped into a rounded ring via <see cref="Region"/>, so the
///     interior is a true hole (clicks fall through, the window shows through)
///     and the corners are rounded to match the window instead of being square.
/// </summary>
public sealed class BorderOverlay : Form
{
    private readonly IntPtr _target;
    private readonly Color _borderColor;
    private readonly int _thickness;

    // Cache the last applied size so the (relatively costly) region is only
    // rebuilt when the target window actually changes size.
    private int _lastW = -1;
    private int _lastH = -1;

    public BorderOverlay(IntPtr target, Color borderColor, int thickness)
    {
        _target = target;
        _borderColor = borderColor;
        _thickness = Math.Max(1, thickness);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Enabled = false;                    // never take focus
        AutoScaleMode = AutoScaleMode.None; // work in raw device pixels
        BackColor = _borderColor;           // the Region clips this to just the ring
        DoubleBuffered = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                        | NativeMethods.WS_EX_TRANSPARENT
                        | NativeMethods.WS_EX_NOACTIVATE
                        | NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    // Do not activate / steal focus when shown.
    protected override bool ShowWithoutActivation => true;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Layered + fully opaque: the window paints its solid BackColor normally,
        // while WS_EX_LAYERED | WS_EX_TRANSPARENT keeps it click-through.
        NativeMethods.SetLayeredWindowAttributes(Handle, 0, 255, NativeMethods.LWA_ALPHA);
    }

    /// <summary>
    /// Re-align the overlay to the current target window rectangle. Hides itself
    /// while the target is minimized or hidden, so no stray border is left behind.
    /// </summary>
    public void UpdatePosition()
    {
        if (!NativeMethods.IsWindow(_target) ||
            NativeMethods.IsIconic(_target) ||
            !NativeMethods.IsWindowVisible(_target))
        {
            if (Visible) Hide();
            return;
        }

        if (!TryGetVisibleRect(_target, out NativeMethods.RECT r))
        {
            if (Visible) Hide();
            return;
        }

        int t = _thickness;
        int x = r.Left - t;
        int y = r.Top - t;
        int w = r.Width + (2 * t);
        int h = r.Height + (2 * t);
        if (w <= 0 || h <= 0)
        {
            if (Visible) Hide();
            return;
        }

        // Rebuild the rounded ring region only when the size changed.
        if (w != _lastW || h != _lastH)
        {
            _lastW = w;
            _lastH = h;
            ApplyRing(w, h, t);
        }

        // Position in physical pixels via SetWindowPos so it lines up regardless
        // of per-monitor DPI scaling.
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, x, y, w, h,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

        if (!Visible) Show();
    }

    /// <summary>Prefer the DWM visible frame bounds; fall back to GetWindowRect.</summary>
    private static bool TryGetVisibleRect(IntPtr hwnd, out NativeMethods.RECT rect)
    {
        int hr = NativeMethods.DwmGetWindowAttribute(
            hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
            out rect, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.RECT>());

        if (hr == 0 && rect.Width > 0 && rect.Height > 0)
            return true;

        return NativeMethods.GetWindowRect(hwnd, out rect);
    }

    /// <summary>
    /// Shape the window into a rounded ring: an outer rounded rectangle with the
    /// inner (window-sized) rounded rectangle punched out.
    /// </summary>
    private void ApplyRing(int w, int h, int t)
    {
        int innerRadius = CornerRadius();
        int outerRadius = innerRadius + t;

        var outer = new Rectangle(0, 0, w, h);
        var inner = new Rectangle(t, t, Math.Max(0, w - 2 * t), Math.Max(0, h - 2 * t));

        using var outerPath = RoundedRect(outer, outerRadius);
        using var innerPath = RoundedRect(inner, innerRadius);

        var region = new Region(outerPath);
        region.Exclude(innerPath);

        Region? old = Region;
        Region = region;
        old?.Dispose();
    }

    /// <summary>Corner radius that tracks the window's own rounded corners (~8 DIP).</summary>
    private int CornerRadius()
    {
        uint dpi = NativeMethods.GetDpiForWindow(_target);
        if (dpi == 0) dpi = 96;
        return (int)Math.Round(8.0 * dpi / 96.0);
    }

    private static GraphicsPath RoundedRect(Rectangle b, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        d = Math.Min(d, Math.Min(b.Width, b.Height));

        if (d <= 0 || b.Width <= 0 || b.Height <= 0)
        {
            if (b.Width > 0 && b.Height > 0) path.AddRectangle(b);
            return path;
        }

        path.AddArc(b.X, b.Y, d, d, 180, 90);
        path.AddArc(b.Right - d, b.Y, d, d, 270, 90);
        path.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
        path.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
