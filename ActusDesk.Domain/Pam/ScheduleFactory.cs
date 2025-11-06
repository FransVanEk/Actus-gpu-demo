namespace ActusDesk.Domain.Pam;

/// <summary>
/// Factory for generating date schedules from ACTUS cycle strings.
/// Supports simplified cycle parsing (e.g., "1M", "3M", "1Y").
/// </summary>
public static class ScheduleFactory
{
    /// <summary>
    /// Generate schedule from anchor date to maturity using cycle string
    /// </summary>
    /// <param name="anchor">Starting date for the schedule</param>
    /// <param name="maturity">End date (inclusive or exclusive based on includeEnd)</param>
    /// <param name="cycle">Cycle string (e.g., "1M", "3M", "6M", "1Y")</param>
    /// <param name="endOfMonthConvention">End of month convention (EOM)</param>
    /// <param name="includeEnd">Whether to include the end date if it falls exactly on a cycle date</param>
    /// <returns>Enumerable of dates in the schedule</returns>
    public static IEnumerable<DateTime> GenerateSchedule(
        DateTime anchor,
        DateTime maturity,
        string cycle,
        string? endOfMonthConvention = null,
        bool includeEnd = true)
    {
        if (string.IsNullOrWhiteSpace(cycle))
            yield break;

        var current = anchor;
        bool isEom = endOfMonthConvention?.Equals("EOM", StringComparison.OrdinalIgnoreCase) == true;

        while (true)
        {
            if (current > maturity)
                break;

            if (current >= anchor)
                yield return current;

            current = AddCycle(current, cycle, isEom);

            // Avoid infinite loop
            if (current > maturity && !includeEnd)
                break;
        }
    }

    /// <summary>
    /// Add a cycle period to a date
    /// </summary>
    private static DateTime AddCycle(DateTime date, string cycle, bool endOfMonth)
    {
        // Parse cycle string: format is like "P3M" or simplified "3M", "1Y", etc.
        string cleanCycle = cycle.TrimStart('P');
        
        if (cleanCycle.EndsWith("M", StringComparison.OrdinalIgnoreCase))
        {
            // Month cycle
            int months = int.Parse(cleanCycle[..^1]);
            var result = date.AddMonths(months);
            
            if (endOfMonth)
            {
                // Adjust to end of month if original date was end of month
                if (IsEndOfMonth(date))
                    result = new DateTime(result.Year, result.Month, DateTime.DaysInMonth(result.Year, result.Month));
            }
            
            return result;
        }
        else if (cleanCycle.EndsWith("Y", StringComparison.OrdinalIgnoreCase))
        {
            // Year cycle
            int years = int.Parse(cleanCycle[..^1]);
            return date.AddYears(years);
        }
        else if (cleanCycle.EndsWith("D", StringComparison.OrdinalIgnoreCase))
        {
            // Day cycle
            int days = int.Parse(cleanCycle[..^1]);
            return date.AddDays(days);
        }
        else if (cleanCycle.EndsWith("W", StringComparison.OrdinalIgnoreCase))
        {
            // Week cycle
            int weeks = int.Parse(cleanCycle[..^1]);
            return date.AddDays(weeks * 7);
        }
        else
        {
            throw new ArgumentException($"Unsupported cycle format: {cycle}");
        }
    }

    private static bool IsEndOfMonth(DateTime date)
    {
        return date.Day == DateTime.DaysInMonth(date.Year, date.Month);
    }
}
