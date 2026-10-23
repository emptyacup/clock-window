using System.Drawing;
using System.Windows.Forms;
using ClockWindow.Models;
using ClockWindow.Services;
using ClockWindow.UI;

namespace ClockWindow;

public class TrayApplicationContext : ApplicationContext
{
    private readonly RuleStore _store = new();
    private readonly List<ReminderRule> _rules;
    private readonly Scheduler _scheduler;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _autoStartItem;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext()
    {
        _rules = _store.Load();

        _scheduler = new Scheduler(() => _rules);
        _scheduler.RuleTriggered += OnRuleTriggered;
        _scheduler.Start();

        _autoStartItem = new ToolStripMenuItem("开机自启", null, OnToggleAutoStart)
        {
            Checked = AutoStartHelper.IsEnabled(),
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("设置规则...", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Icon = BuildTrayIcon(),
            Text = "ClockWindow 提醒",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        if (!AutoStartHelper.IsEnabled())
        {
            AutoStartHelper.SetEnabled(true);
            _autoStartItem.Checked = true;
        }
    }

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_store, _rules);
        _settingsForm.Show();
    }

    private void OnRuleTriggered(ReminderRule rule)
    {
        if (rule.Messages.Count == 0) return;
        var message = rule.Messages[Random.Shared.Next(rule.Messages.Count)];
        var popup = new ReminderPopupForm(rule.Title, message);
        popup.Show();
    }

    private void OnToggleAutoStart(object? sender, EventArgs e)
    {
        var enabled = !_autoStartItem.Checked;
        AutoStartHelper.SetEnabled(enabled);
        _autoStartItem.Checked = enabled;
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        _scheduler.Dispose();
        Application.Exit();
    }

    private static Icon BuildTrayIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            WindowChrome.DrawAppIcon(g, new Rectangle(1, 1, 30, 30));
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
