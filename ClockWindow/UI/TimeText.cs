using System.Globalization;

namespace ClockWindow.UI;

/// <summary>编辑窗口时间输入框的 "HH:mm" 文本与 TimeSpan 互转（调度只精确到分钟）。</summary>
internal static class TimeText
{
    private static readonly string[] Formats = { "H:mm", "HH:mm", "H:m", "HHmm" };

    public static string Format(TimeSpan time) => $"{time.Hours:00}:{time.Minutes:00}";

    public static bool TryParse(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // 允许中文输入法下打出来的全角冒号
        var normalized = text.Trim().Replace('：', ':');
        if (!DateTime.TryParseExact(normalized, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return false;

        time = parsed.TimeOfDay;
        return true;
    }
}
