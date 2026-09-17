using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AlwaysOnTop;

/// <summary>
/// Shared visual language for the app's windows: a minimalist, "bento grid"
/// layout built from rounded cards, on a warm off-white canvas, with a deep
/// navy and a bright lime as the two brand colors.
/// </summary>
internal static class Theme
{
    // ---- Palette ----------------------------------------------------------
    public static readonly Color Canvas     = Color.FromArgb(0xF4, 0xF5, 0xF1); // warm off-white
    public static readonly Color Card       = Color.FromArgb(0xFF, 0xFF, 0xFF);
    public static readonly Color CardBorder = Color.FromArgb(0xE7, 0xE9, 0xE2);

    public static readonly Color Navy       = Color.FromArgb(0x1B, 0x2A, 0x4A); // deep navy
    public static readonly Color NavyDeep   = Color.FromArgb(0x14, 0x20, 0x3A);
    public static readonly Color NavySoft   = Color.FromArgb(0x2A, 0x3B, 0x60);

    public static readonly Color Lime       = Color.FromArgb(0xCE, 0xE6, 0x4E); // bright lime accent
    public static readonly Color LimeDeep   = Color.FromArgb(0x9A, 0xB8, 0x2C); // lime for text on light

    public static readonly Color Ink        = Color.FromArgb(0x1A, 0x22, 0x33); // primary text
    public static readonly Color Muted      = Color.FromArgb(0x6C, 0x74, 0x82); // secondary text
    public static readonly Color OnNavy      = Color.FromArgb(0xED, 0xF0, 0xF6); // text on navy
    public static readonly Color OnNavyMuted = Color.FromArgb(0xA9, 0xB4, 0xCC);

    public static readonly Color FieldBorder = Color.FromArgb(0xD4, 0xD8, 0xCE);

    // ---- Fonts ------------------------------------------------------------
    public static Font Display(float size, FontStyle style = FontStyle.Bold)
        => new("Segoe UI Semibold", size, style, GraphicsUnit.Point);
    public static Font Body(float size = 9.75f, FontStyle style = FontStyle.Regular)
        => new("Segoe UI", size, style, GraphicsUnit.Point);

    // ---- Geometry ---------------------------------------------------------
    public static GraphicsPath RoundedRect(Rectangle b, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        d = Math.Min(d, Math.Min(b.Width, b.Height));
        if (d <= 0)
        {
            path.AddRectangle(b);
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

/// <summary>A rounded "bento" card with a subtle border, painted with anti-aliasing.</summary>
internal class CardPanel : Panel
{
    public int Radius { get; set; } = 14;
    public Color BorderColor { get; set; } = Theme.CardBorder;
    public int BorderWidth { get; set; } = 1;

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.UserPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Blend the rounded corners with whatever is behind us.
        Color behind = Parent?.BackColor ?? Theme.Canvas;
        g.Clear(behind);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(r, Radius);
        using var fill = new SolidBrush(BackColor);
        g.FillPath(fill, path);
        if (BorderWidth > 0)
        {
            using var pen = new Pen(BorderColor, BorderWidth);
            g.DrawPath(pen, path);
        }
    }
}

/// <summary>A flat, rounded "pill" button with hover feedback. Used for actions.</summary>
internal class PillButton : Button
{
    public int Radius { get; set; } = 11;
    public Color FillColor { get; set; } = Theme.Navy;
    public Color HoverColor { get; set; } = Theme.NavySoft;
    public Color OutlineColor { get; set; } = Color.Empty; // Empty => no outline
    private bool _hover;

    public PillButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.UserPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        ForeColor = Theme.OnNavy;
        Font = Theme.Body(10f, FontStyle.Bold);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Canvas);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(r, Radius);
        using var fill = new SolidBrush(_hover ? HoverColor : FillColor);
        g.FillPath(fill, path);
        if (OutlineColor != Color.Empty)
        {
            using var pen = new Pen(OutlineColor, 1.4f);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, Text, Font, r, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            | TextFormatFlags.EndEllipsis);
    }
}
