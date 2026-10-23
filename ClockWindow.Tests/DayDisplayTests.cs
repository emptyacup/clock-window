using ClockWindow.Models;
using ClockWindow.UI;
using Xunit;

namespace ClockWindow.Tests;

public class DayDisplayTests
{
    [Fact]
    public void FormatAbbrev_NoDays_ReturnsDash()
    {
        Assert.Equal("-", DayDisplay.FormatAbbrev(Days.None));
    }

    [Fact]
    public void FormatAbbrev_SingleDay_ReturnsThatDayAbbrev()
    {
        Assert.Equal("五", DayDisplay.FormatAbbrev(Days.Friday));
    }

    [Fact]
    public void FormatAbbrev_MultipleDays_ReturnsInWeekOrder()
    {
        var days = Days.Friday | Days.Monday | Days.Sunday;
        Assert.Equal("一 五 日", DayDisplay.FormatAbbrev(days));
    }

    [Fact]
    public void FlagsAndFullNames_HaveSevenEntriesInMondayToSundayOrder()
    {
        Assert.Equal(7, DayDisplay.Flags.Length);
        Assert.Equal(7, DayDisplay.FullNames.Length);
        Assert.Equal(Days.Monday, DayDisplay.Flags[0]);
        Assert.Equal(Days.Sunday, DayDisplay.Flags[6]);
        Assert.Equal("周一", DayDisplay.FullNames[0]);
        Assert.Equal("周日", DayDisplay.FullNames[6]);
    }
}
