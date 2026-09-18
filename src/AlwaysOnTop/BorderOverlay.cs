using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// A frameless, click-through, top-most overlay that draws a colored border
/// hugging a pinned window - the visual cue PowerToys shows around pinned
/// windows.
///
/// It is a per-pixel-alpha layered window painted with <c>UpdateLayeredWindow</c>:
///   * The inner area is fully transparent, so the pinned window always shows
///     through and dragging other windows over the region never leaves the
///     pinned window's content unpainted (the classic plain-WS_EX_TRANSPARENT
///     artifact of "content gone, only the border left").
///   * The frame is an anti-aliased rounded rectangle whose corner radius tracks
///     the window DPI, matching Windows 11 rounded windows.
///   * It positions itself from <c>DWMWA_EXTENDED_FRAME_BOUNDS</c> (the visible
///     frame) so the border hugs the window exactly.
/// </summary>
public sealed class BorderOverlay : Form
{
    private readonly IntPtr _target;
    private readonly Color _borderColor;
    private readonly int _thickness;

    private int _lastX = int.MinValue, _lastY = int.MinValue;
    private int _lastW = -1, _lastH = -1;

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
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED     // required for UpdateLayeredWindow
                        | NativeMethods.WS_EX_TRANSPARENT // click-through
                        | NativeMethods.WS_EX_NOACTIVATE
                        | NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    // Do not activate / steal focus when shown.
    protected override bool ShowWithoutActivation => true;

    // The window content comes entirely from UpdateLayeredWindow.
    protected override void OnPaintBackground(PaintEventArgs e) { }
    protected override void OnPaint(PaintEventArgs e) { }

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
            // Size changed: rebuild the ring bitmap and blit it (also positions).
            Redraw(x, y, w, h);
        }
        else if (x != _lastX || y != _lastY)
        {
            // Position only: move the layered window; its content is preserved.
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
        _lastW = _lastH = -1; // force a redraw when it reappears
    }

    /// <summary>Build the ring bitmap and push it to the window via UpdateLayeredWindow.</summary>
    private void Redraw(int x, int y, int w, int h)
    {
        using Bitmap bmp = BuildRingBitmap(w, h, _thickness, CornerRadius(), _borderColor);

        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
        IntPtr hBmp = bmp.GetHbitmap(Color.FromArgb(0));
        IntPtr oldObj = NativeMethods.SelectObject(memDc, hBmp);
        try
        {
            var ptDst = new NativeMethods.POINT(x, y);
            var size = new NativeMethods.SIZE(w, h);
            var ptSrc = new NativeMethods.POINT(0, 0);
            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };
            NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref ptDst, ref size,
                memDc, ref ptSrc, 0, ref blend, NativeMethods.ULW_ALPHA);
        }
        finally
        {
            NativeMethods.SelectObject(memDc, oldObj);
            NativeMethods.DeleteObject(hBmp);
            NativeMethods.DeleteDC(memDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
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

    /// <summary>
    /// A 32bpp premultiplied-alpha bitmap holding an anti-aliased rounded border
    /// stroke; the interior is fully transparent.
    /// </summary>
    private static Bitmap BuildRingBitmap(int w, int h, int t, int radius, Color color)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Stroke the mid-line of the frame with a pen of width t so the outer
            // edge sits at 0 and the inner edge at t.
            var mid = new Rectangle(t / 2, t / 2, Math.Max(1, w - t), Math.Max(1, h - t));
            using var path = Theme.RoundedRect(mid, Math.Max(1, radius + t / 2));
            using var pen = new Pen(color, t)
            {
                Alignment = PenAlignment.Center,
                LineJoin = LineJoin.Round
            };
            g.DrawPath(pen, path);
        }

        Premultiply(bmp);
        return bmp;
    }

    /// <summary>Premultiply RGB by alpha, as UpdateLayeredWindow (AC_SRC_ALPHA) expects.</summary>
    private static void Premultiply(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            int bytes = Math.Abs(data.Stride) * bmp.Height;
            byte[] buf = new byte[bytes];
            Marshal.Copy(data.Scan0, buf, 0, bytes);

            for (int i = 0; i < bytes; i += 4) // memory order: B, G, R, A
            {
                byte a = buf[i + 3];
                if (a == 255) continue;
                if (a == 0) { buf[i] = buf[i + 1] = buf[i + 2] = 0; continue; }
                buf[i]     = (byte)(buf[i]     * a / 255);
                buf[i + 1] = (byte)(buf[i + 1] * a / 255);
                buf[i + 2] = (byte)(buf[i + 2] * a / 255);
            }

            Marshal.Copy(buf, 0, data.Scan0, bytes);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    /// <summary>Corner radius that tracks the window's own rounded corners (~8 DIP).</summary>
    private int CornerRadius()
    {
        uint dpi = NativeMethods.GetDpiForWindow(_target);
        if (dpi == 0) dpi = 96;
        return (int)Math.Round(8.0 * dpi / 96.0);
    }
}
