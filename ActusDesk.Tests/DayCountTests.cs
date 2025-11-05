using ActusDesk.Domain;
using ActusDesk.Domain.DayCounts;

namespace ActusDesk.Tests;

public class DayCountTests
{
    [Fact]
    public void Act360_CalculatesCorrectYearFraction()
    {
        // Arrange
        var dayCount = new Act360DayCount();
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 12, 31);

        // Act
        double yearFrac = dayCount.YearFrac(start, end);

        // Assert
        Assert.Equal(365.0 / 360.0, yearFrac, precision: 6);
    }

    [Fact]
    public void Act365F_CalculatesCorrectYearFraction()
    {
        // Arrange
        var dayCount = new Act365FDayCount();
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 12, 31);

        // Act
        double yearFrac = dayCount.YearFrac(start, end);

        // Assert
        Assert.Equal(365.0 / 365.0, yearFrac, precision: 6);
    }

    [Fact]
    public void Thirty360_CalculatesCorrectYearFraction()
    {
        // Arrange
        var dayCount = new Thirty360DayCount();
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 12, 30);

        // Act
        double yearFrac = dayCount.YearFrac(start, end);

        // Assert
        // 30/360: 11 months * 30 + 29 days = 359 days
        Assert.Equal(359.0 / 360.0, yearFrac, precision: 6);
    }

    [Fact]
    public void DayCountFactory_CreatesCorrectImplementation()
    {
        // Arrange & Act
        var act360 = DayCountFactory.Create(DayCountConvention.Act360);
        var act365 = DayCountFactory.Create(DayCountConvention.Act365F);
        var thirty360 = DayCountFactory.Create(DayCountConvention.Thirty360);

        // Assert
        Assert.IsType<Act360DayCount>(act360);
        Assert.IsType<Act365FDayCount>(act365);
        Assert.IsType<Thirty360DayCount>(thirty360);
    }
}
