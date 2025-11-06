using ActusDesk.Domain;
using ActusDesk.Domain.Calendars;

namespace ActusDesk.Tests;

public class CalendarTests
{
    [Fact]
    public void Following_AdjustsWeekendToMonday()
    {
        // Arrange
        var adjuster = new BusinessDayAdjuster();
        var saturday = new DateOnly(2024, 11, 9); // Saturday

        // Act
        var adjusted = adjuster.Adjust(saturday, BusinessDayConvention.Following, CalendarType.None);

        // Assert
        Assert.Equal(new DateOnly(2024, 11, 11), adjusted); // Monday
        Assert.Equal(DayOfWeek.Monday, adjusted.DayOfWeek);
    }

    [Fact]
    public void Preceding_AdjustsWeekendToFriday()
    {
        // Arrange
        var adjuster = new BusinessDayAdjuster();
        var sunday = new DateOnly(2024, 11, 10); // Sunday

        // Act
        var adjusted = adjuster.Adjust(sunday, BusinessDayConvention.Preceding, CalendarType.None);

        // Assert
        Assert.Equal(new DateOnly(2024, 11, 8), adjusted); // Friday
        Assert.Equal(DayOfWeek.Friday, adjusted.DayOfWeek);
    }

    [Fact]
    public void ModifiedFollowing_StaysInSameMonth()
    {
        // Arrange
        var adjuster = new BusinessDayAdjuster();
        var endOfMonthSaturday = new DateOnly(2024, 11, 30); // Saturday

        // Act
        var adjusted = adjuster.Adjust(endOfMonthSaturday, BusinessDayConvention.ModifiedFollowing, CalendarType.None);

        // Assert
        Assert.Equal(11, adjusted.Month); // Still November
        Assert.Equal(DayOfWeek.Friday, adjusted.DayOfWeek);
    }

    [Fact]
    public void None_DoesNotAdjust()
    {
        // Arrange
        var adjuster = new BusinessDayAdjuster();
        var saturday = new DateOnly(2024, 11, 9);

        // Act
        var adjusted = adjuster.Adjust(saturday, BusinessDayConvention.None, CalendarType.None);

        // Assert
        Assert.Equal(saturday, adjusted);
    }
}
