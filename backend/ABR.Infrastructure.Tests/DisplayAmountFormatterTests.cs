using ABR.Infrastructure.Formatting;

namespace ABR.Infrastructure.Tests;

public class DisplayAmountFormatterTests
{
    [Fact]
    public void Format_ScalesExampleProfit()
    {
        Assert.Equal("363861.552", DisplayAmountFormatter.Format(363861552m));
    }

    [Fact]
    public void Format_TrimsTrailingZeros()
    {
        Assert.Equal("1500", DisplayAmountFormatter.Format(1_500_000m));
        Assert.Equal("67", DisplayAmountFormatter.Format(67_000m));
    }

    [Fact]
    public void Format_HandlesZeroAndNull()
    {
        Assert.Equal("0", DisplayAmountFormatter.Format(0m));
        Assert.Equal(string.Empty, DisplayAmountFormatter.Format((decimal?)null));
    }

    [Fact]
    public void Format_KeepsPaisaPrecision()
    {
        Assert.Equal("0.00001", DisplayAmountFormatter.Format(0.01m));
    }

    [Fact]
    public void Scale_DividesByThousand()
    {
        Assert.Equal(363861.552m, DisplayAmountFormatter.Scale(363861552m));
    }
}
