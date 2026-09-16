using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// A frameless, click-through, top-most overlay that draws a colored border
/// tracking a pinned window - the visual cue PowerToys shows around pinned
/// windows. The inner area is painted with the transparency key so only the
/// border frame is visible and mouse clicks pass through to the window below.
/// </summary>
public sealed class BorderOverlay : Form
{
    private readonly IntPtr _target;
    private readonly Color _borderColor;
    private readonly int _thickness;

    public BorderOverlay(IntPtr target, Color borderColor, int thickness)
    {
        _target = target;
        _borderColor = borderColor;
        _thickness = Math.Max(1, thickness);

        // Use a transparency key that cannot collide with the chosen border color.
        Color key = _borderColor.ToArgb() == Color.Magenta.ToArgb() ? Color.Lime : Color.Magenta;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Enabled = false;                 // never take focus
        AutoScaleMode = AutoScaleMode.None; // work in raw device pixels
        BackColor = _borderColor;
        TransparencyKey = key;
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

    // Do not activate/steal focus when shown.
    protected override bool ShowWithoutActivation => true;

    /// <summary>Re-align the overlay to the current target window rectangle.</summary>
    public void UpdatePosition()
    {
        if (!NativeMethods.GetWindowRect(_target, out NativeMethods.RECT r))
            return;

        int t = _thickness;
        int x = r.Left - t;
        int y = r.Top - t;
        int w = r.Width + (2 * t);
        int h = r.Height + (2 * t);

        // Position in physical pixels via SetWindowPos so it lines up regardless
        // of per-monitor DPI scaling.
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, x, y, w, h,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Fill the interior with the transparency key so only the frame shows.
        int t = _thickness;
        var inner = new Rectangle(t, t,
            Math.Max(0, Width - (2 * t)),
            Math.Max(0, Height - (2 * t)));
        using var brush = new SolidBrush(TransparencyKey);
        e.Graphics.FillRectangle(brush, inner);
    }
}
