using ClockWindow.Models;

namespace ClockWindow.UI;

internal static class DayDisplay
{
    public static readonly string[] Abbrev = { "一", "二", "三", "四", "五", "六", "日" };
    public static readonly string[] FullNames = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
    public static readonly Days[] Flags =
    {
        Days.Monday, Days.Tuesday, Days.Wednesday, Days.Thursday,
        Days.Friday, Days.Saturday, Days.Sunday,
    };

    public static string FormatAbbrev(Days days)
    {
        var parts = new List<string>();
        for (int i = 0; i < Flags.Length; i++)
        {
            if ((days & Flags[i]) != 0) parts.Add(Abbrev[i]);
        }
        return parts.Count == 0 ? "-" : string.Join(" ", parts);
    }
}
