using OAI.Domain.ValueObjects;

namespace OAI.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_ShouldNormalizeCurrencyToUppercase()
    {
        var money = new Money(1000m, "vnd");

        Assert.Equal("VND", money.Currency);
    }

    [Fact]
    public void Constructor_ShouldRoundAmountToTwoDecimals()
    {
        var money = new Money(10.235m, "VND");

        Assert.Equal(10.24m, money.Amount);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new Money(1000m, ""));
    }

    [Fact]
    public void Add_ShouldReturnTotal_WhenSameCurrency()
    {
        var left = new Money(1000m, "VND");
        var right = new Money(500m, "VND");

        var result = left + right;

        Assert.Equal(1500m, result.Amount);
        Assert.Equal("VND", result.Currency);
    }

    [Fact]
    public void Add_ShouldThrow_WhenCurrencyMismatch()
    {
        var left = new Money(1000m, "VND");
        var right = new Money(1m, "USD");

        Assert.Throws<InvalidOperationException>(() => left + right);
    }

    [Fact]
    public void IsCloseTo_ShouldReturnTrue_WhenDifferenceIsWithinTolerance()
    {
        var left = new Money(1000.00m, "VND");
        var right = new Money(1000.01m, "VND");

        var result = left.IsCloseTo(right);

        Assert.True(result);
    }
}