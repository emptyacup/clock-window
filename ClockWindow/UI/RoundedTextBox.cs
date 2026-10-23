using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClockWindow.UI;

internal class RoundedTextBox : Panel
{
    // 聚焦时外圈画一道 3px 的浅色光环，所以真正的输入框边框往里缩这么多
    private const float RingWidth = 3f;

    private bool _focused;

    public TextBox InnerTextBox { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new string Text
    {
        get => InnerTextBox.Text;
        set => InnerTextBox.Text = value;
    }

    public RoundedTextBox(bool multiline = false)
    {
        BackColor = Theme.Surface;
        Padding = multiline ? new Padding(14, 11, 8, 11) : new Padding(14, 0, 14, 0);
        Cursor = Cursors.IBeam;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        InnerTextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Multiline = multiline,
            Font = Theme.Font(9.5f),
            ForeColor = Theme.TextPrimary,
            BackColor = Theme.Surface,
            ScrollBars = ScrollBars.None,
        };
        if (multiline)
        {
            InnerTextBox.Dock = DockStyle.Fill;
            InnerTextBox.TextChanged += (_, _) => UpdateScrollBar();
            InnerTextBox.Resize += (_, _) => UpdateScrollBar();
        }
        else
        {
            // 单行 TextBox 高度由字体决定、不能拉伸，所以不用 Dock，手动垂直居中
            Resize += (_, _) => CenterSingleLine();
        }
        InnerTextBox.Enter += (_, _) => { _focused = true; Invalidate(); };
        InnerTextBox.Leave += (_, _) => { _focused = false; Invalidate(); };
        Click += (_, _) => InnerTextBox.Focus();
        Controls.Add(InnerTextBox);
    }

    // 系统滚动条是灰色经典样式，常驻显示很突兀，所以只在内容超出可视高度时才显示
    private void UpdateScrollBar()
    {
        var textBox = InnerTextBox;
        // 预留滚动条宽度来量，避免出现滚动条后换行变多、又刚好不需要滚动条的来回抖动
        int measureWidth = Math.Max(1, textBox.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
        int textHeight = TextRenderer.MeasureText(textBox.Text + "\n", textBox.Font, new Size(measureWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height;
        var wanted = textHeight > textBox.ClientSize.Height ? ScrollBars.Vertical : ScrollBars.None;
        if (textBox.ScrollBars == wanted) return;

        // 修改 ScrollBars 会重建 TextBox 句柄，输入中的焦点和光标位置要手动还原
        bool focused = textBox.Focused;
        int selectionStart = textBox.SelectionStart, selectionLength = textBox.SelectionLength;
        textBox.ScrollBars = wanted;
        textBox.Select(selectionStart, selectionLength);
        if (focused)
        {
            textBox.Focus();
            textBox.ScrollToCaret();
        }
    }

    private void CenterSingleLine()
    {
        InnerTextBox.Width = Width - Padding.Horizontal;
        InnerTextBox.Location = new Point(Padding.Left, (Height - InnerTextBox.Height) / 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_focused)
        {
            using var ringPath = RoundedPathFactory.Create(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Theme.RadiusMedium + RingWidth - 1);
            using var ringPen = new Pen(Theme.AccentRing, RingWidth);
            g.DrawPath(ringPen, ringPath);
        }

        var boxRect = new RectangleF(RingWidth - 0.5f, RingWidth - 0.5f, Width - RingWidth * 2 + 1f, Height - RingWidth * 2 + 1f);
        using var boxPath = RoundedPathFactory.Create(boxRect, Theme.RadiusMedium);
        using (var fill = new SolidBrush(Theme.Surface))
        {
            g.FillPath(fill, boxPath);
        }
        using var pen = new Pen(_focused ? Theme.AccentStart : Theme.Border, 1f);
        g.DrawPath(pen, boxPath);
    }
}
