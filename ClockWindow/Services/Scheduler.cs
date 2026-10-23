using System;
using ClockWindow.Models;

namespace ClockWindow.Services;

public class Scheduler : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Func<List<ReminderRule>> _getRules;
    private readonly Dictionary<Guid, DateTime> _lastFiredDate = new();

    public event Action<ReminderRule>? RuleTriggered;

    public Scheduler(Func<List<ReminderRule>> getRules)
    {
        _getRules = getRules;
        _timer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _timer.Tick += OnTick;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var today = now.Date;

        foreach (var rule in _getRules())
        {
            if (!rule.Enabled) continue;
            if (!rule.MatchesDay(now.DayOfWeek)) continue;
            if (now.Hour != rule.Time.Hours || now.Minute != rule.Time.Minutes) continue;

            if (_lastFiredDate.TryGetValue(rule.Id, out var lastDate) && lastDate == today)
                continue;

            _lastFiredDate[rule.Id] = today;
            RuleTriggered?.Invoke(rule);
        }
    }

    public void Dispose()
    {
        _timer.Tick -= OnTick;
        _timer.Dispose();
    }
}
