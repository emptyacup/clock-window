using System;

namespace ClockWindow.Models;

[Flags]
public enum Days
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
}

public class ReminderRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public List<string> Messages { get; set; } = new();
    public Days DaysOfWeek { get; set; } = Days.None;
    public TimeSpan Time { get; set; } = new TimeSpan(9, 0, 0);
    public bool Enabled { get; set; } = true;

    public bool MatchesDay(DayOfWeek day)
    {
        var flag = day switch
        {
            DayOfWeek.Monday => Days.Monday,
            DayOfWeek.Tuesday => Days.Tuesday,
            DayOfWeek.Wednesday => Days.Wednesday,
            DayOfWeek.Thursday => Days.Thursday,
            DayOfWeek.Friday => Days.Friday,
            DayOfWeek.Saturday => Days.Saturday,
            DayOfWeek.Sunday => Days.Sunday,
            _ => Days.None,
        };
        return (DaysOfWeek & flag) != 0;
    }
}
