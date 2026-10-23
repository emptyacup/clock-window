using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal class CapsuleToggle : Control
{
    private bool _checked;
    private bool _hover;

    public event EventHandler? CheckedChanged;

    [DefaultValue(false)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public CapsuleToggle(string text)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                  ControlStyles.SupportsTransparentBackColor, true);
        Text = text;
        Size = new Size(44, 32);
        Font = Theme.Font(9f);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
        Click += (_, _) => Checked = !Checked;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = RoundedPathFactory.Create(rect, Theme.RadiusMedium);

        Color textColor;
        if (_checked)
        {
            var start = _hover ? Theme.Lighten(Theme.AccentStart, 0.1f) : Theme.AccentStart;
            var end = _hover ? Theme.Lighten(Theme.AccentEnd, 0.1f) : Theme.AccentEnd;
            using var brush = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), start, end, 0f);
            g.FillPath(brush, path);
            textColor = Color.White;
        }
        else
        {
            using var brush = new SolidBrush(_hover ? Theme.SurfaceHover : Theme.Surface);
            g.FillPath(brush, path);
            using var pen = new Pen(_hover ? Theme.BorderStrong : Theme.Border, 1f);
            g.DrawPath(pen, path);
            textColor = Theme.TextSecondary;
        }

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}
