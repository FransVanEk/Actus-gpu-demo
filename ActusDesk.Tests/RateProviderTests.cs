using ActusDesk.Domain.Rates;

namespace ActusDesk.Tests;

public class RateProviderTests
{
    [Fact]
    public void ConstantRateProvider_ReturnsConstantRate()
    {
        // Arrange
        var provider = new ConstantRateProvider(0.05f);

        // Act
        float rate = provider.GetRate("USD", 12, new DateOnly(2024, 1, 1));

        // Assert
        Assert.Equal(0.05f, rate);
    }

    [Fact]
    public void RateCurve_ExactTenorMatch_ReturnsExactRate()
    {
        // Arrange
        var curve = new RateCurve(new DateOnly(2024, 1, 1));
        curve.AddPoint(12, 0.03f);
        curve.AddPoint(24, 0.035f);
        curve.AddPoint(36, 0.04f);

        // Act
        float rate = curve.GetRate(24, new DateOnly(2024, 1, 1));

        // Assert
        Assert.Equal(0.035f, rate);
    }

    [Fact]
    public void RateCurve_Interpolation_ReturnsInterpolatedRate()
    {
        // Arrange
        var curve = new RateCurve(new DateOnly(2024, 1, 1));
        curve.AddPoint(12, 0.03f);
        curve.AddPoint(36, 0.05f);

        // Act
        float rate = curve.GetRate(24, new DateOnly(2024, 1, 1)); // Midpoint

        // Assert
        // Linear interpolation: 0.03 + 0.5 * (0.05 - 0.03) = 0.04
        Assert.Equal(0.04f, rate, precision: 6);
    }

    [Fact]
    public void RateCurve_FlatCurve_ReturnsConstantRate()
    {
        // Arrange
        var curve = RateCurve.CreateFlatCurve(new DateOnly(2024, 1, 1), 0.045f);

        // Act
        float rate1 = curve.GetRate(12, new DateOnly(2024, 1, 1));
        float rate2 = curve.GetRate(120, new DateOnly(2024, 1, 1));

        // Assert
        Assert.Equal(0.045f, rate1);
        Assert.Equal(0.045f, rate2);
    }
}
