using ClockWindow.UI;
using Xunit;

namespace ClockWindow.Tests;

public class TimeTextTests
{
    [Theory]
    [InlineData("17:00", 17, 0)]
    [InlineData("9:05", 9, 5)]
    [InlineData(" 08:30 ", 8, 30)]
    [InlineData("17：00", 17, 0)]
    [InlineData("1700", 17, 0)]
    [InlineData("0:00", 0, 0)]
    [InlineData("23:59", 23, 59)]
    public void TryParse_ValidInput_ReturnsTime(string text, int hours, int minutes)
    {
        Assert.True(TimeText.TryParse(text, out var time));
        Assert.Equal(new TimeSpan(hours, minutes, 0), time);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("24:00")]
    [InlineData("12:60")]
    [InlineData("abc")]
    [InlineData("12:00:00")]
    public void TryParse_InvalidInput_ReturnsFalse(string? text)
    {
        Assert.False(TimeText.TryParse(text, out _));
    }

    [Fact]
    public void Format_DropsSecondsAndPadsZeros()
    {
        Assert.Equal("09:05", TimeText.Format(new TimeSpan(9, 5, 30)));
    }
}
