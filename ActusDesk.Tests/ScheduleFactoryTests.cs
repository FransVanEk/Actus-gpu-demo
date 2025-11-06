using ActusDesk.Domain.Pam;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for schedule generation from cycle strings
/// </summary>
public class ScheduleFactoryTests
{
    [Fact]
    public void ScheduleFactory_MonthCycle_GeneratesCorrectDates()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 7, 1);
        var cycle = "3M";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Equal(3, schedule.Count);
        Assert.Equal(new DateTime(2024, 1, 1), schedule[0]);
        Assert.Equal(new DateTime(2024, 4, 1), schedule[1]);
        Assert.Equal(new DateTime(2024, 7, 1), schedule[2]);
    }

    [Fact]
    public void ScheduleFactory_YearCycle_GeneratesCorrectDates()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2027, 1, 1);
        var cycle = "1Y";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Equal(4, schedule.Count);
        Assert.Equal(new DateTime(2024, 1, 1), schedule[0]);
        Assert.Equal(new DateTime(2025, 1, 1), schedule[1]);
        Assert.Equal(new DateTime(2026, 1, 1), schedule[2]);
        Assert.Equal(new DateTime(2027, 1, 1), schedule[3]);
    }

    [Fact]
    public void ScheduleFactory_DayCycle_GeneratesCorrectDates()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 1, 31);
        var cycle = "10D";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Equal(4, schedule.Count);
        Assert.Equal(new DateTime(2024, 1, 1), schedule[0]);
        Assert.Equal(new DateTime(2024, 1, 11), schedule[1]);
        Assert.Equal(new DateTime(2024, 1, 21), schedule[2]);
        Assert.Equal(new DateTime(2024, 1, 31), schedule[3]);
    }

    [Fact]
    public void ScheduleFactory_WeekCycle_GeneratesCorrectDates()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 2, 1);
        var cycle = "2W";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.True(schedule.Count >= 2);
        Assert.Equal(new DateTime(2024, 1, 1), schedule[0]);
        Assert.Equal(new DateTime(2024, 1, 15), schedule[1]);
    }

    [Fact]
    public void ScheduleFactory_WithPCycle_ParsesCorrectly()
    {
        // Arrange - ISO 8601 duration format with P prefix
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 7, 1);
        var cycle = "P3M";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Equal(3, schedule.Count);
        Assert.Equal(new DateTime(2024, 1, 1), schedule[0]);
        Assert.Equal(new DateTime(2024, 4, 1), schedule[1]);
    }

    [Fact]
    public void ScheduleFactory_QuarterlyCycle_GeneratesCorrectSchedule()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2025, 1, 1);
        var cycle = "3M";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Equal(5, schedule.Count); // Q1, Q2, Q3, Q4, and final
        Assert.Equal(new DateTime(2024, 1, 1), schedule[0]);
        Assert.Equal(new DateTime(2024, 4, 1), schedule[1]);
        Assert.Equal(new DateTime(2024, 7, 1), schedule[2]);
        Assert.Equal(new DateTime(2024, 10, 1), schedule[3]);
        Assert.Equal(new DateTime(2025, 1, 1), schedule[4]);
    }

    [Fact]
    public void ScheduleFactory_SemiAnnualCycle_GeneratesCorrectSchedule()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2026, 1, 1);
        var cycle = "6M";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Equal(5, schedule.Count);
        Assert.Equal(new DateTime(2024, 1, 1), schedule[0]);
        Assert.Equal(new DateTime(2024, 7, 1), schedule[1]);
        Assert.Equal(new DateTime(2025, 1, 1), schedule[2]);
        Assert.Equal(new DateTime(2025, 7, 1), schedule[3]);
        Assert.Equal(new DateTime(2026, 1, 1), schedule[4]);
    }

    [Fact]
    public void ScheduleFactory_StopsAtMaturity()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 8, 15);
        var cycle = "3M";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.All(schedule, date => Assert.True(date <= maturity));
        // Should have Q1, Q2, Q3 (but not Q4 since it would be after maturity)
        Assert.Equal(3, schedule.Count);
    }

    [Fact]
    public void ScheduleFactory_EmptyCycle_ReturnsEmpty()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 12, 31);
        var cycle = "";

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Empty(schedule);
    }

    [Fact]
    public void ScheduleFactory_NullCycle_ReturnsEmpty()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 12, 31);
        string? cycle = null;

        // Act
        var schedule = ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList();

        // Assert
        Assert.Empty(schedule);
    }

    [Fact]
    public void ScheduleFactory_InvalidCycle_ThrowsException()
    {
        // Arrange
        var anchor = new DateTime(2024, 1, 1);
        var maturity = new DateTime(2024, 12, 31);
        var cycle = "X";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            ScheduleFactory.GenerateSchedule(anchor, maturity, cycle).ToList());
    }
}
