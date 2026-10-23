using System.Drawing;
using System.Windows.Forms;
using ClockWindow.Models;

namespace ClockWindow.UI;

public class RuleEditForm : ChromelessForm
{
    private const int ContentLeft = 24;
    private const int ContentWidth = 352;
    private const int FooterHeight = 64;

    private readonly RoundedTextBox _titleBox;
    private readonly CapsuleToggle[] _dayToggles;
    private readonly RoundedTextBox _timeBox;
    private readonly RoundedTextBox _messagesBox;
    private readonly ToggleSwitch _enabledToggle;

    public ReminderRule Rule { get; }

    public RuleEditForm(ReminderRule rule) : base(resizable: false, minimizable: false)
    {
        Rule = rule;

        Text = "编辑提醒规则";
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = Theme.Surface;
        ClientSize = new Size(ContentLeft * 2 + ContentWidth, 540);

        // 输入框四周预留了 3px 聚焦光环，所以坐标统一往外扩 3px，视觉上和标签左对齐
        int y = TitleBarHeight + 12;
        Controls.Add(NewLabel("标题", ContentLeft, y));
        _titleBox = new RoundedTextBox { Location = new Point(ContentLeft - 3, y + 22), Size = new Size(ContentWidth + 6, 42), Text = rule.Title };
        _titleBox.InnerTextBox.PlaceholderText = "例如：周五祝福";
        Controls.Add(_titleBox);

        y += 80;
        Controls.Add(NewLabel("重复", ContentLeft, y));
        _dayToggles = new CapsuleToggle[7];
        const int chipGap = 6;
        int chipWidth = (ContentWidth - chipGap * 6) / 7;
        for (int i = 0; i < 7; i++)
        {
            _dayToggles[i] = new CapsuleToggle(DayDisplay.FullNames[i])
            {
                Location = new Point(ContentLeft + i * (chipWidth + chipGap), y + 25),
                Size = new Size(chipWidth, 32),
                Checked = (rule.DaysOfWeek & DayDisplay.Flags[i]) != 0,
            };
            Controls.Add(_dayToggles[i]);
        }

        y += 76;
        const int enabledColumn = ContentLeft + 180;
        Controls.Add(NewLabel("触发时间", ContentLeft, y));
        Controls.Add(NewLabel("启用此规则", enabledColumn, y));
        _timeBox = new RoundedTextBox { Location = new Point(ContentLeft - 3, y + 22), Size = new Size(126, 42), Text = TimeText.Format(rule.Time) };
        _timeBox.InnerTextBox.PlaceholderText = "HH:mm";
        Controls.Add(_timeBox);
        _enabledToggle = new ToggleSwitch { Location = new Point(enabledColumn, y + 33), Checked = rule.Enabled };
        Controls.Add(_enabledToggle);

        y += 80;
        var messagesLabel = NewLabel("文案库", ContentLeft, y);
        Controls.Add(messagesLabel);
        // AutoSize 的 Label 要等布局后才有准确宽度，这里直接量文字宽度来定位提示文字
        int messagesLabelWidth = TextRenderer.MeasureText(messagesLabel.Text, messagesLabel.Font).Width;
        Controls.Add(new Label
        {
            Text = "每行一条，触发时随机选一条",
            Location = new Point(ContentLeft + messagesLabelWidth + 2, y + 1),
            AutoSize = true,
            ForeColor = Theme.TextMuted,
            Font = Theme.Font(8.5f),
        });
        _messagesBox = new RoundedTextBox(multiline: true)
        {
            Location = new Point(ContentLeft - 3, y + 22),
            Size = new Size(ContentWidth + 6, ClientSize.Height - FooterHeight - (y + 22) - 16),
            Text = string.Join(Environment.NewLine, rule.Messages),
        };
        Controls.Add(_messagesBox);

        int buttonTop = ClientSize.Height - FooterHeight + (FooterHeight - 34) / 2;
        var okButton = new ModernButton("保存", ButtonVariant.Primary) { DialogResult = DialogResult.OK };
        var cancelButton = new ModernButton("取消", ButtonVariant.Secondary) { DialogResult = DialogResult.Cancel };
        okButton.Location = new Point(ClientSize.Width - ContentLeft - okButton.Width, buttonTop);
        cancelButton.Location = new Point(okButton.Left - 8 - cancelButton.Width, buttonTop);
        okButton.Click += OnSave;

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    private static Label NewLabel(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        AutoSize = true,
        ForeColor = Theme.TextPrimary,
        Font = Theme.Font(9f, FontStyle.Bold),
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        // 底部按钮区用浅灰底 + 顶部细线和内容区分开
        var footerRect = new Rectangle(0, ClientSize.Height - FooterHeight, ClientSize.Width, FooterHeight);
        using (var brush = new SolidBrush(Theme.FooterBackground))
        {
            e.Graphics.FillRectangle(brush, footerRect);
        }
        using (var pen = new Pen(Theme.Border, 1f))
        {
            e.Graphics.DrawLine(pen, 0, footerRect.Top, ClientSize.Width, footerRect.Top);
        }
        base.OnPaint(e);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_titleBox.Text))
        {
            MessageBox.Show(this, "标题不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var messages = _messagesBox.Text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim())
            .Where(m => m.Length > 0)
            .ToList();

        if (messages.Count == 0)
        {
            MessageBox.Show(this, "文案库不能为空，至少填写一条", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var days = Days.None;
        for (int i = 0; i < _dayToggles.Length; i++)
        {
            if (_dayToggles[i].Checked) days |= DayDisplay.Flags[i];
        }

        if (days == Days.None)
        {
            MessageBox.Show(this, "至少选择一个重复星期", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (!TimeText.TryParse(_timeBox.Text, out var time))
        {
            MessageBox.Show(this, "触发时间格式不正确，请按 HH:mm 填写，例如 17:00", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Rule.Title = _titleBox.Text.Trim();
        Rule.Messages = messages;
        Rule.DaysOfWeek = days;
        Rule.Time = time;
        Rule.Enabled = _enabledToggle.Checked;
    }
}
