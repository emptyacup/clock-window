using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

public class ReminderPopupForm : Form
{
    private const int PopupWidth = 400;
    private const int PanelTop = 150;
    // 装饰图和底部白色文字区之间用一小段渐变过渡，不是一条硬边；太长会盖住装饰图的下半部分
    private const int FadeHeight = 14;
    private const int Side = 22;
    private const int CornerRadius = 14;
    private const int TitleRowTop = PanelTop + 12;
    private const int TitleRowHeight = 34;
    private const int MessageMaxLines = 2;
    private const int BottomPadding = 18;
    private const TextFormatFlags MessageFlags =
        TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

    private static readonly List<ReminderPopupForm> OpenPopups = new();
    private static readonly Font TitleFont = Theme.Font(12.5f, FontStyle.Bold);
    private static readonly Font BodyFont = Theme.Font(9.5f);

    private readonly string _title;
    private readonly string _message;
    private readonly Rectangle _messageRect;
    private readonly string _timeText = DateTime.Now.ToString("HH:mm");
    // 顶部装饰图在创建时随机生成一次，之后每次重绘直接贴图
    private readonly Bitmap _decoration;
    private readonly System.Windows.Forms.Timer _autoCloseTimer;
    private readonly System.Windows.Forms.Timer _fadeInTimer;
    private readonly Rectangle _closeRect;
    private bool _closeHover;

    public ReminderPopupForm(string title, string message)
    {
        _title = title;
        _message = message;

        // 文字区高度按正文实际行数（最多两行）来定，短文案时不留大片空白
        int messageWidth = PopupWidth - Side * 2;
        int lineHeight = TextRenderer.MeasureText("测", BodyFont).Height;
        int measured = TextRenderer.MeasureText(message, BodyFont, new Size(messageWidth, int.MaxValue), MessageFlags).Height;
        int messageHeight = Math.Clamp(measured, lineHeight, lineHeight * MessageMaxLines);
        _messageRect = new Rectangle(Side - 1, TitleRowTop + TitleRowHeight + 2, messageWidth, messageHeight);

        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(PopupWidth, _messageRect.Bottom + BottomPadding);
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        BackColor = Theme.Surface;
        Opacity = 0;

        _closeRect = new Rectangle(ClientSize.Width - 14 - 28, 14, 28, 28);

        _decoration = PopupDecoration.Render(new Size(ClientSize.Width, PanelTop));

        Paint += DrawContent;

        // 按钮和标题放在同一行右侧，省掉单独一行按钮栏
        var button = new ModernButton("知道了", ButtonVariant.Primary)
        {
            Size = new Size(84, TitleRowHeight),
            Location = new Point(ClientSize.Width - Side - 84, TitleRowTop),
        };
        button.Click += (_, _) => Close();
        Controls.Add(button);

        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 300_000 };
        _autoCloseTimer.Tick += (_, _) => { _autoCloseTimer.Stop(); Close(); };
        _autoCloseTimer.Start();

        _fadeInTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _fadeInTimer.Tick += (_, _) =>
        {
            Opacity = Math.Min(1, Opacity + 0.12);
            if (Opacity >= 1) _fadeInTimer.Stop();
        };

        Shown += (_, _) => { Activate(); _fadeInTimer.Start(); };
        FormClosed += OnFormClosed;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= WindowChrome.CS_DROPSHADOW;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowChrome.ApplyRoundedCorners(this, CornerRadius);
    }

    private void DrawContent(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        g.DrawImageUnscaled(_decoration, 0, 0);

        var fadeRect = new Rectangle(0, PanelTop - FadeHeight, ClientSize.Width, FadeHeight);
        using (var fadeBrush = new LinearGradientBrush(fadeRect, Color.FromArgb(0, Theme.Surface), Theme.Surface, 90f))
        {
            // 渐变矩形比填充区域多出 1px，避免 GDI+ 渐变在边缘处回绕出一条细线
            g.FillRectangle(fadeBrush, fadeRect.X, fadeRect.Y + 1, fadeRect.Width, fadeRect.Height - 1);
        }
        using (var panelBrush = new SolidBrush(Theme.Surface))
        {
            g.FillRectangle(panelBrush, 0, PanelTop, ClientSize.Width, ClientSize.Height - PanelTop);
        }

        DrawTimeChip(g);
        DrawCloseButton(g);

        // 标题右侧让出按钮的位置
        TextRenderer.DrawText(g, _title, TitleFont, new Rectangle(Side - 2, TitleRowTop, ClientSize.Width - Side * 2 - 84 - 12, TitleRowHeight),
            Theme.TextPrimary, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, _message, BodyFont, _messageRect, Theme.TextSecondary, MessageFlags);

        WindowChrome.DrawBorder(g, ClientSize, CornerRadius);
    }

    private void DrawTimeChip(Graphics g)
    {
        using var font = Theme.NumberFont(9f);
        var textSize = TextRenderer.MeasureText(_timeText, font);
        var chipRect = new Rectangle(14, 14, textSize.Width + 26, 28);
        using (var path = RoundedPathFactory.Create(new RectangleF(chipRect.X, chipRect.Y, chipRect.Width, chipRect.Height), chipRect.Height / 2f))
        using (var brush = new SolidBrush(Color.FromArgb(225, Theme.Surface)))
        {
            g.FillPath(brush, path);
        }

        var dotRect = new RectangleF(chipRect.X + 10, chipRect.Y + chipRect.Height / 2f - 3, 6, 6);
        using (var dotBrush = new LinearGradientBrush(dotRect, Theme.AccentStart, Theme.AccentEnd, 45f))
        {
            g.FillEllipse(dotBrush, dotRect);
        }
        TextRenderer.DrawText(g, _timeText, font, new Rectangle(chipRect.X + 18, chipRect.Y, chipRect.Width - 20, chipRect.Height),
            Theme.TextPrimary, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private void DrawCloseButton(Graphics g)
    {
        using (var brush = new SolidBrush(Color.FromArgb(_closeHover ? 255 : 210, Theme.Surface)))
        {
            g.FillEllipse(brush, _closeRect);
        }
        using var pen = new Pen(_closeHover ? Theme.TextPrimary : Theme.TextSecondary, 1.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        float cx = _closeRect.X + _closeRect.Width / 2f, cy = _closeRect.Y + _closeRect.Height / 2f, r = 4f;
        g.DrawLine(pen, cx - r, cy - r, cx + r, cy + r);
        g.DrawLine(pen, cx + r, cy - r, cx - r, cy + r);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hover = _closeRect.Contains(e.Location);
        if (hover == _closeHover) return;
        _closeHover = hover;
        Cursor = hover ? Cursors.Hand : Cursors.Default;
        Invalidate(_closeRect);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_closeHover) return;
        _closeHover = false;
        Cursor = Cursors.Default;
        Invalidate(_closeRect);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && _closeRect.Contains(e.Location)) Close();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Location = NextPosition();
        OpenPopups.Add(this);
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        OpenPopups.Remove(this);
        _autoCloseTimer.Stop();
        _autoCloseTimer.Dispose();
        _fadeInTimer.Stop();
        _fadeInTimer.Dispose();
        _decoration.Dispose();
    }

    private Point NextPosition()
    {
        var area = Screen.PrimaryScreen!.WorkingArea;
        var stackOffset = OpenPopups.Count * (Height + 12);
        return new Point(area.Right - Width - 24, area.Top + 24 + stackOffset);
    }
}
