namespace ActusDesk.Domain.Calendars;

/// <summary>
/// Implements business day adjustment logic
/// </summary>
public sealed class BusinessDayAdjuster : IBusinessDayAdjuster
{
    private readonly Dictionary<CalendarType, HashSet<DateOnly>> _holidays;

    public BusinessDayAdjuster()
    {
        _holidays = new Dictionary<CalendarType, HashSet<DateOnly>>();
        InitializeCalendars();
    }

    public DateOnly Adjust(DateOnly date, BusinessDayConvention convention, CalendarType calendar)
    {
        if (convention == BusinessDayConvention.None)
            return date;

        return convention switch
        {
            BusinessDayConvention.Following => AdjustFollowing(date, calendar),
            BusinessDayConvention.ModifiedFollowing => AdjustModifiedFollowing(date, calendar),
            BusinessDayConvention.Preceding => AdjustPreceding(date, calendar),
            _ => date
        };
    }

    private DateOnly AdjustFollowing(DateOnly date, CalendarType calendar)
    {
        while (IsHoliday(date, calendar) || IsWeekend(date))
        {
            date = date.AddDays(1);
        }
        return date;
    }

    private DateOnly AdjustPreceding(DateOnly date, CalendarType calendar)
    {
        while (IsHoliday(date, calendar) || IsWeekend(date))
        {
            date = date.AddDays(-1);
        }
        return date;
    }

    private DateOnly AdjustModifiedFollowing(DateOnly date, CalendarType calendar)
    {
        int originalMonth = date.Month;
        DateOnly adjusted = AdjustFollowing(date, calendar);
        
        // If adjustment moved to next month, use preceding instead
        if (adjusted.Month != originalMonth)
        {
            adjusted = AdjustPreceding(date, calendar);
        }
        
        return adjusted;
    }

    private bool IsWeekend(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;
        return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
    }

    private bool IsHoliday(DateOnly date, CalendarType calendar)
    {
        if (calendar == CalendarType.None || !_holidays.ContainsKey(calendar))
            return false;

        return _holidays[calendar].Contains(date);
    }

    private void InitializeCalendars()
    {
        // TARGET calendar (European Central Bank)
        _holidays[CalendarType.TARGET] = new HashSet<DateOnly>
        {
            // Fixed holidays
            new DateOnly(2024, 1, 1),   // New Year's Day
            new DateOnly(2024, 12, 25), // Christmas
            new DateOnly(2024, 12, 26), // Boxing Day
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 25),
            new DateOnly(2025, 12, 26),
            // Add more as needed
        };

        // New York calendar (Federal Reserve)
        _holidays[CalendarType.NewYork] = new HashSet<DateOnly>
        {
            new DateOnly(2024, 1, 1),   // New Year's Day
            new DateOnly(2024, 7, 4),   // Independence Day
            new DateOnly(2024, 12, 25), // Christmas
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 7, 4),
            new DateOnly(2025, 12, 25),
            // Add more as needed
        };

        // London calendar (Bank of England)
        _holidays[CalendarType.London] = new HashSet<DateOnly>
        {
            new DateOnly(2024, 1, 1),   // New Year's Day
            new DateOnly(2024, 12, 25), // Christmas
            new DateOnly(2024, 12, 26), // Boxing Day
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 25),
            new DateOnly(2025, 12, 26),
            // Add more as needed
        };
    }

    /// <summary>
    /// Load custom holidays from file
    /// </summary>
    public void LoadCustomCalendar(string filePath)
    {
        // TODO: Implement loading holidays from JSON/CSV file
    }
}
