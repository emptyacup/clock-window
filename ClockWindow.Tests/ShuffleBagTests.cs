using ClockWindow.UI;
using Xunit;

namespace ClockWindow.Tests;

public class ShuffleBagTests
{
    [Fact]
    public void Next_OneRound_ReturnsEveryItemExactlyOnce()
    {
        var bag = new ShuffleBag<int>(Enumerable.Range(0, 20), new Random(1));

        var round = Enumerable.Range(0, 20).Select(_ => bag.Next()).ToList();

        Assert.Equal(Enumerable.Range(0, 20), round.OrderBy(x => x));
    }

    [Fact]
    public void Next_AcrossRounds_NeverRepeatsBackToBack()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var bag = new ShuffleBag<int>(new[] { 1, 2, 3 }, new Random(seed));
            int previous = bag.Next();
            for (int i = 0; i < 30; i++)
            {
                int current = bag.Next();
                Assert.NotEqual(previous, current);
                previous = current;
            }
        }
    }

    [Fact]
    public void Next_SingleItem_AlwaysReturnsIt()
    {
        var bag = new ShuffleBag<string>(new[] { "only" }, new Random(0));

        Assert.Equal("only", bag.Next());
        Assert.Equal("only", bag.Next());
    }

    [Fact]
    public void Next_Empty_Throws()
    {
        var bag = new ShuffleBag<int>(Array.Empty<int>(), new Random(0));

        Assert.Throws<InvalidOperationException>(() => bag.Next());
    }
}
