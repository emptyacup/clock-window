using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

/// <summary>
/// 无系统边框的窗口基类：自绘标题栏（图标 + 标题 + 最小化/关闭）、拖动、可选的边缘缩放、阴影和圆角。
/// 子类把内容控件放在 <see cref="TitleBarHeight"/> 以下即可。
/// </summary>
public class ChromelessForm : Form
{
    protected const int TitleBarHeight = 44;
    private const int CornerRadius = 10;
    private const int ResizeGrip = 6;

    private const int WM_NCHITTEST = 0x84;
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_SYSMENU = 0x00080000;

    private readonly bool _resizable;
    private readonly bool _minimizable;

    protected ChromelessForm(bool resizable, bool minimizable)
    {
        _resizable = resizable;
        _minimizable = minimizable;

        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = minimizable;
        DoubleBuffered = true;
        Font = Theme.Font(9f);
        BackColor = Theme.WindowBackground;

        var closeButton = new TitleBarButton(TitleBarGlyph.Close) { Anchor = AnchorStyles.Top | AnchorStyles.Right };
        closeButton.Click += (_, _) => Close();
        Controls.Add(closeButton);

        if (minimizable)
        {
            var minimizeButton = new TitleBarButton(TitleBarGlyph.Minimize) { Anchor = AnchorStyles.Top | AnchorStyles.Right };
            minimizeButton.Click += (_, _) => WindowState = FormWindowState.Minimized;
            Controls.Add(minimizeButton);
        }

        // ClientSize 由子类在构造函数里设置，此时才知道最终宽度，所以在 Load 时再摆放标题栏按钮
        Load += (_, _) => LayoutTitleBarButtons();
    }

    private void LayoutTitleBarButtons()
    {
        int x = ClientSize.Width - 8;
        foreach (var button in Controls.OfType<TitleBarButton>())
        {
            x -= button.Width;
            button.Location = new Point(x, (TitleBarHeight - button.Height) / 2);
            x -= 2;
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= WindowChrome.CS_DROPSHADOW;
            // 无边框窗口默认不响应"点击任务栏图标最小化"，补上这两个样式位
            if (_minimizable) cp.Style |= WS_MINIMIZEBOX | WS_SYSMENU;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowChrome.ApplyRoundedCorners(this, CornerRadius);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate(new Rectangle(0, 0, ClientSize.Width, TitleBarHeight));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        // 子控件区域的命中测试由子控件自己处理，这里只会收到窗口自身客户区（标题栏空白、边缘）的消息
        if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
        {
            long lParam = m.LParam.ToInt64();
            var point = PointToClient(new Point(unchecked((short)lParam), unchecked((short)(lParam >> 16))));
            m.Result = (IntPtr)HitTest(point);
        }
    }

    private int HitTest(Point p)
    {
        if (_resizable && WindowState == FormWindowState.Normal)
        {
            bool left = p.X < ResizeGrip, right = p.X >= ClientSize.Width - ResizeGrip;
            bool top = p.Y < ResizeGrip, bottom = p.Y >= ClientSize.Height - ResizeGrip;
            if (top && left) return HTTOPLEFT;
            if (top && right) return HTTOPRIGHT;
            if (bottom && left) return HTBOTTOMLEFT;
            if (bottom && right) return HTBOTTOMRIGHT;
            if (left) return HTLEFT;
            if (right) return HTRIGHT;
            if (top) return HTTOP;
            if (bottom) return HTBOTTOM;
        }
        return p.Y < TitleBarHeight ? HTCAPTION : HTCLIENT;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        WindowChrome.DrawAppIcon(g, new Rectangle(16, (TitleBarHeight - 20) / 2, 20, 20));
        using var titleFont = Theme.Font(9f, FontStyle.Bold);
        TextRenderer.DrawText(g, Text, titleFont, new Rectangle(44, 0, ClientSize.Width - 140, TitleBarHeight),
            Theme.TextSecondary, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        WindowChrome.DrawBorder(g, ClientSize, CornerRadius);
    }
}

internal enum TitleBarGlyph { Close, Minimize }

internal class TitleBarButton : Control
{
    private readonly TitleBarGlyph _glyph;
    private bool _hover;

    public TitleBarButton(TitleBarGlyph glyph)
    {
        _glyph = glyph;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.Selectable, false);
        Size = new Size(32, 28);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_hover)
        {
            using var path = RoundedPathFactory.Create(new RectangleF(0, 0, Width, Height), Theme.RadiusSmall);
            using var brush = new SolidBrush(_glyph == TitleBarGlyph.Close ? Theme.DangerSoft : Theme.GhostHover);
            g.FillPath(brush, path);
        }

        var color = _hover && _glyph == TitleBarGlyph.Close ? Theme.Danger : Theme.TextSecondary;
        using var pen = new Pen(color, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float cx = Width / 2f, cy = Height / 2f, r = 4.5f;
        if (_glyph == TitleBarGlyph.Close)
        {
            g.DrawLine(pen, cx - r, cy - r, cx + r, cy + r);
            g.DrawLine(pen, cx + r, cy - r, cx - r, cy + r);
        }
        else
        {
            g.DrawLine(pen, cx - r, cy, cx + r, cy);
        }
    }
}
