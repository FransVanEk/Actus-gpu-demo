namespace ActusDesk.Domain.DayCounts;

/// <summary>
/// Implements ACT/360 day count convention
/// </summary>
public sealed class Act360DayCount : IDayCount
{
    public double YearFrac(DateOnly start, DateOnly end)
    {
        int days = end.DayNumber - start.DayNumber;
        return days / 360.0;
    }
}

/// <summary>
/// Implements ACT/365F day count convention (Fixed 365 days per year)
/// </summary>
public sealed class Act365FDayCount : IDayCount
{
    public double YearFrac(DateOnly start, DateOnly end)
    {
        int days = end.DayNumber - start.DayNumber;
        return days / 365.0;
    }
}

/// <summary>
/// Implements 30/360 day count convention (30E/360 European)
/// </summary>
public sealed class Thirty360DayCount : IDayCount
{
    public double YearFrac(DateOnly start, DateOnly end)
    {
        int y1 = start.Year, m1 = start.Month, d1 = start.Day;
        int y2 = end.Year, m2 = end.Month, d2 = end.Day;
        
        // 30E/360 adjustments
        if (d1 == 31) d1 = 30;
        if (d2 == 31) d2 = 30;
        
        int days = 360 * (y2 - y1) + 30 * (m2 - m1) + (d2 - d1);
        return days / 360.0;
    }
}

/// <summary>
/// Implements ACT/ACT ISDA day count convention
/// </summary>
public sealed class ActActISDADayCount : IDayCount
{
    public double YearFrac(DateOnly start, DateOnly end)
    {
        if (start.Year == end.Year)
        {
            int daysInYear = DateTime.IsLeapYear(start.Year) ? 366 : 365;
            int days = end.DayNumber - start.DayNumber;
            return days / (double)daysInYear;
        }
        
        // Multi-year: sum fractions for each year
        double yearFrac = 0.0;
        DateOnly current = start;
        
        while (current.Year < end.Year)
        {
            DateOnly yearEnd = new DateOnly(current.Year, 12, 31);
            int daysInYear = DateTime.IsLeapYear(current.Year) ? 366 : 365;
            int days = yearEnd.DayNumber - current.DayNumber + 1;
            yearFrac += days / (double)daysInYear;
            current = new DateOnly(current.Year + 1, 1, 1);
        }
        
        if (current < end)
        {
            int daysInYear = DateTime.IsLeapYear(end.Year) ? 366 : 365;
            int days = end.DayNumber - current.DayNumber;
            yearFrac += days / (double)daysInYear;
        }
        
        return yearFrac;
    }
}

/// <summary>
/// Factory for day count conventions
/// </summary>
public static class DayCountFactory
{
    public static IDayCount Create(DayCountConvention convention) => convention switch
    {
        DayCountConvention.Act360 => new Act360DayCount(),
        DayCountConvention.Act365F => new Act365FDayCount(),
        DayCountConvention.Thirty360 => new Thirty360DayCount(),
        DayCountConvention.Thirty360E => new Thirty360DayCount(),
        DayCountConvention.ActActISDA => new ActActISDADayCount(),
        DayCountConvention.ActActICMA => new ActActISDADayCount(), // Simplified
        _ => throw new ArgumentException($"Unknown day count convention: {convention}")
    };
}
