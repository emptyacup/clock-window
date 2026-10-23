using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ClockWindow.Models;

namespace ClockWindow.UI;

internal class RuleRowControl : Panel
{
    private const int RowHeight = 68;
    private const int TimeColumnWidth = 84;

    private static readonly Font TimeFont = Theme.NumberFont(15f);
    private static readonly Font TitleFont = Theme.Font(10f, FontStyle.Bold);
    private static readonly Font SubtitleFont = Theme.Font(8.5f);

    public ReminderRule Rule { get; }
    public ToggleSwitch EnabledToggle { get; }

    private bool _selected;
    private bool _hover;

    [DefaultValue(false)]
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Invalidate();
        }
    }

    public RuleRowControl(ReminderRule rule)
    {
        Rule = rule;
        Size = new Size(0, RowHeight);
        Margin = new Padding(0, 0, 0, 10);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        EnabledToggle = new ToggleSwitch { Checked = rule.Enabled };
        // 开关状态影响标题/时间的颜色（停用时置灰）
        EnabledToggle.CheckedChanged += (_, _) => Invalidate();
        Controls.Add(EnabledToggle);

        Resize += (_, _) =>
            EnabledToggle.Location = new Point(Width - EnabledToggle.Width - 18, (Height - EnabledToggle.Height) / 2);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        // 鼠标移到行内的开关上也会触发父控件的 MouseLeave，这种情况仍算悬停
        if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
        {
            _hover = false;
            Invalidate();
        }
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.WindowBackground);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var cardRect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (var path = RoundedPathFactory.Create(cardRect, Theme.RadiusLarge))
        {
            using var fill = new SolidBrush(_selected ? Theme.AccentSoft : _hover ? Theme.SurfaceHover : Theme.Surface);
            g.FillPath(fill, path);
            var borderColor = _selected ? Theme.AccentBorder : _hover ? Theme.BorderStrong : Theme.Border;
            using var pen = new Pen(borderColor, _selected ? 1.5f : 1f);
            g.DrawPath(pen, path);
        }

        // 选中态左侧一条渐变竖条
        if (_selected)
        {
            var barRect = new RectangleF(0, 18, 3, Height - 36);
            using var barPath = RoundedPathFactory.Create(barRect, 1.5f);
            using var barBrush = new LinearGradientBrush(new RectangleF(0, barRect.Y - 1, 3, barRect.Height + 2),
                Theme.AccentStart, Theme.AccentEnd, 90f);
            g.FillPath(barBrush, barPath);
        }

        bool enabled = EnabledToggle.Checked;
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;

        var time = DateTime.Today.Add(Rule.Time).ToString("HH:mm");
        TextRenderer.DrawText(g, time, TimeFont, new Rectangle(18, 0, TimeColumnWidth, Height),
            enabled ? Theme.TextPrimary : Theme.TextMuted, flags);

        using (var dividerPen = new Pen(Theme.Border, 1f))
        {
            g.DrawLine(dividerPen, TimeColumnWidth + 4, 20, TimeColumnWidth + 4, Height - 20);
        }

        int textLeft = TimeColumnWidth + 20;
        int textWidth = Math.Max(0, EnabledToggle.Left - textLeft - 12);
        TextRenderer.DrawText(g, Rule.Title, TitleFont, new Rectangle(textLeft, 13, textWidth, 22),
            enabled ? Theme.TextPrimary : Theme.TextMuted, flags);
        TextRenderer.DrawText(g, Subtitle(), SubtitleFont, new Rectangle(textLeft, 36, textWidth, 20),
            Theme.TextMuted, flags);
    }

    private string Subtitle()
    {
        var days = DayDisplay.FormatAbbrev(Rule.DaysOfWeek);
        var repeat = days switch
        {
            "-" => "未设置重复",
            "一 二 三 四 五 六 日" => "每天",
            _ => "每周" + days.Replace(" ", "、"),
        };
        return $"{repeat} · {Rule.Messages.Count} 条文案";
    }
}
