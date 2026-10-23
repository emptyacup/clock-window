using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal enum ButtonVariant
{
    /// <summary>渐变实心，主操作</summary>
    Primary,
    /// <summary>白底细描边，次要操作</summary>
    Secondary,
    /// <summary>无底色，悬停才出现浅灰底，工具栏操作</summary>
    Ghost,
}

/// <summary>
/// 自绘抗锯齿圆角按钮。实现 IButtonControl，可以作为 Form.AcceptButton / CancelButton，
/// 点击时和系统 Button 一样先把 DialogResult 写给所在窗口，再触发 Click 事件。
/// </summary>
internal class ModernButton : Control, IButtonControl
{
    private bool _hover;
    private bool _pressed;

    public ButtonVariant Variant { get; }

    [DefaultValue(DialogResult.None)]
    public DialogResult DialogResult { get; set; }

    public ModernButton(string text, ButtonVariant variant)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable |
                 ControlStyles.StandardClick, true);
        Text = text;
        Variant = variant;
        Size = new Size(84, 34);
        Font = Theme.Font(9.5f, variant == ButtonVariant.Primary ? FontStyle.Bold : FontStyle.Regular);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    public void NotifyDefault(bool value) { }

    public void PerformClick()
    {
        if (CanSelect) OnClick(EventArgs.Empty);
    }

    protected override void OnClick(EventArgs e)
    {
        var form = FindForm();
        if (form != null && DialogResult != DialogResult.None) form.DialogResult = DialogResult;
        base.OnClick(e);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter) PerformClick();
        base.OnKeyUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = RoundedPathFactory.Create(rect, Theme.RadiusMedium);

        Color textColor;
        switch (Variant)
        {
            case ButtonVariant.Primary:
            {
                var start = Theme.AccentStart;
                var end = Theme.AccentEnd;
                if (!Enabled) { start = Theme.Lighten(start, 0.5f); end = Theme.Lighten(end, 0.5f); }
                else if (_pressed) { start = Theme.Darken(start, 0.12f); end = Theme.Darken(end, 0.12f); }
                else if (_hover) { start = Theme.Lighten(start, 0.12f); end = Theme.Lighten(end, 0.12f); }

                using var brush = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), start, end, 0f);
                g.FillPath(brush, path);
                // 顶部一道很淡的高光，让按钮有一点立体感
                using var highlight = new Pen(Color.FromArgb(Enabled ? 50 : 20, Color.White), 1f);
                g.DrawLine(highlight, Theme.RadiusMedium, 1.5f, Width - Theme.RadiusMedium, 1.5f);
                textColor = Color.White;
                break;
            }
            case ButtonVariant.Secondary:
            {
                var fill = _pressed ? Theme.GhostHover : _hover ? Theme.SurfaceHover : Theme.Surface;
                using var brush = new SolidBrush(fill);
                g.FillPath(brush, path);
                using var pen = new Pen(_hover && Enabled ? Theme.BorderStrong : Theme.Border, 1f);
                g.DrawPath(pen, path);
                textColor = Enabled ? Theme.TextPrimary : Theme.TextMuted;
                break;
            }
            default:
            {
                if (Enabled && (_hover || _pressed))
                {
                    using var brush = new SolidBrush(_pressed ? Theme.GhostPressed : Theme.GhostHover);
                    g.FillPath(brush, path);
                }
                textColor = Enabled ? (_hover ? Theme.TextPrimary : Theme.TextSecondary) : Theme.TextMuted;
                break;
            }
        }

        if (Focused && ShowFocusCues)
        {
            using var ringPath = RoundedPathFactory.Create(new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f), Theme.RadiusMedium - 1);
            using var ringPen = new Pen(Variant == ButtonVariant.Primary ? Color.FromArgb(160, Color.White) : Theme.AccentBorder, 1.5f);
            g.DrawPath(ringPen, ringPath);
        }

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}
