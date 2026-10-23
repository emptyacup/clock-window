using System.Drawing;
using System.Windows.Forms;
using ClockWindow.Models;
using ClockWindow.Services;

namespace ClockWindow.UI;

public class SettingsForm : ChromelessForm
{
    private const int ContentLeft = 24;
    private const int HeaderTop = TitleBarHeight + 12;
    private const int ListTop = HeaderTop + 64;
    private const int FooterHeight = 60;

    private readonly RuleStore _store;
    private readonly List<ReminderRule> _rules;
    private readonly FlowLayoutPanel _rowsPanel;
    private readonly ModernButton _editButton;
    private readonly ModernButton _deleteButton;
    private readonly ModernButton _testButton;
    private RuleRowControl? _selectedRow;

    public SettingsForm(RuleStore store, List<ReminderRule> rules) : base(resizable: true, minimizable: true)
    {
        _store = store;
        _rules = rules;

        Text = "ClockWindow";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(580, 480);
        MinimumSize = new Size(480, 380);

        var addButton = new ModernButton("+  新增规则", ButtonVariant.Primary)
        {
            Size = new Size(112, 36),
            Location = new Point(ClientSize.Width - ContentLeft - 112, HeaderTop + 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        addButton.Click += OnAdd;
        Controls.Add(addButton);

        _rowsPanel = new FlowLayoutPanel
        {
            Location = new Point(ContentLeft, ListTop),
            Size = new Size(ClientSize.Width - ContentLeft * 2, ClientSize.Height - ListTop - FooterHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.WindowBackground,
        };
        _rowsPanel.Resize += (_, _) =>
        {
            foreach (Control c in _rowsPanel.Controls) c.Width = _rowsPanel.ClientSize.Width - 4;
            _rowsPanel.Invalidate();
        };
        _rowsPanel.Paint += DrawEmptyState;
        Controls.Add(_rowsPanel);

        int footerButtonTop = ClientSize.Height - FooterHeight + 14;
        _editButton = NewToolbarButton("编辑", ContentLeft - 8, footerButtonTop, 64);
        _deleteButton = NewToolbarButton("删除", _editButton.Right + 4, footerButtonTop, 64);
        _testButton = NewToolbarButton("立即测试", _deleteButton.Right + 4, footerButtonTop, 88);

        _editButton.Click += (_, _) => EditSelected();
        _deleteButton.Click += OnDelete;
        _testButton.Click += OnTest;

        RefreshList();
    }

    private ModernButton NewToolbarButton(string text, int x, int y, int width)
    {
        var button = new ModernButton(text, ButtonVariant.Ghost)
        {
            Location = new Point(x, y),
            Size = new Size(width, 32),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        };
        Controls.Add(button);
        return button;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;

        using var headingFont = Theme.Font(15f, FontStyle.Bold);
        using var summaryFont = Theme.Font(9f);
        TextRenderer.DrawText(g, "提醒规则", headingFont, new Point(ContentLeft - 3, HeaderTop), Theme.TextPrimary);

        int enabledCount = _rules.Count(r => r.Enabled);
        var summary = _rules.Count == 0 ? "还没有任何规则" : $"共 {_rules.Count} 条规则，{enabledCount} 条已启用";
        TextRenderer.DrawText(g, summary, summaryFont, new Point(ContentLeft - 1, HeaderTop + 34), Theme.TextMuted);

        int footerTop = ClientSize.Height - FooterHeight;
        using (var pen = new Pen(Theme.Border, 1f))
        {
            g.DrawLine(pen, ContentLeft, footerTop, ClientSize.Width - ContentLeft, footerTop);
        }
        TextRenderer.DrawText(g, "双击规则可快速编辑", summaryFont,
            new Rectangle(0, footerTop, ClientSize.Width - ContentLeft, FooterHeight),
            Theme.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private void DrawEmptyState(object? sender, PaintEventArgs e)
    {
        if (_rowsPanel.Controls.Count > 0) return;
        using var font = Theme.Font(9.5f);
        TextRenderer.DrawText(e.Graphics, "点击右上角「新增规则」创建第一条提醒", font,
            _rowsPanel.ClientRectangle, Theme.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private void RefreshList()
    {
        // Snapshot before disposing: Control.Dispose() removes itself from its parent's
        // Controls collection as a side effect, so disposing while foreach-enumerating
        // _rowsPanel.Controls directly would mutate the collection mid-iteration and
        // silently skip every other row (verified: with the live collection, only 1 of
        // every 2 old rows actually got disposed).
        foreach (Control c in _rowsPanel.Controls.Cast<Control>().ToList()) c.Dispose();
        _rowsPanel.Controls.Clear();
        _selectedRow = null;
        foreach (var rule in _rules)
        {
            var row = new RuleRowControl(rule) { Width = _rowsPanel.ClientSize.Width - 4 };
            row.Click += (_, _) => SelectRow(row);
            row.DoubleClick += (_, _) => { SelectRow(row); EditSelected(); };
            row.EnabledToggle.CheckedChanged += (_, _) =>
            {
                rule.Enabled = row.EnabledToggle.Checked;
                _store.Save(_rules);
                Invalidate();
            };
            _rowsPanel.Controls.Add(row);
        }
        _rowsPanel.Invalidate();
        UpdateSelectionActions();
        Invalidate();
    }

    private void SelectRow(RuleRowControl row)
    {
        if (_selectedRow is { IsDisposed: false }) _selectedRow.Selected = false;
        _selectedRow = row;
        row.Selected = true;
        UpdateSelectionActions();
    }

    private void UpdateSelectionActions()
    {
        bool hasSelection = _selectedRow != null;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
        _testButton.Enabled = hasSelection;
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        var rule = new ReminderRule();
        using var form = new RuleEditForm(rule);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _rules.Add(rule);
            _store.Save(_rules);
            RefreshList();
        }
    }

    private void EditSelected()
    {
        if (_selectedRow == null) return;
        var rule = _selectedRow.Rule;

        using var form = new RuleEditForm(rule);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _store.Save(_rules);
            RefreshList();
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_selectedRow == null) return;
        var rule = _selectedRow.Rule;

        var confirm = MessageBox.Show(this, $"确定删除规则「{rule.Title}」吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _rules.Remove(rule);
        _store.Save(_rules);
        RefreshList();
    }

    private void OnTest(object? sender, EventArgs e)
    {
        if (_selectedRow == null) return;
        var rule = _selectedRow.Rule;
        if (rule.Messages.Count == 0) return;

        var message = rule.Messages[Random.Shared.Next(rule.Messages.Count)];
        var popup = new ReminderPopupForm(rule.Title, message);
        popup.Show();
    }
}
