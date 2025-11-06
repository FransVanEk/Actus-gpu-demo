namespace ActusDesk.Domain.Pam;

/// <summary>
/// Concrete implementation of IPamScenario supporting multiple event types
/// Applies rate shocks and value adjustments based on scenario events
/// </summary>
public sealed class PamScenario : IPamScenario
{
    private readonly List<ScenarioEventDefinition> _events;

    public string Name { get; }
    public string? Description { get; }

    public PamScenario(string name, string? description = null)
    {
        Name = name;
        Description = description;
        _events = new List<ScenarioEventDefinition>();
    }

    public void AddEvent(ScenarioEventDefinition eventDef)
    {
        _events.Add(eventDef);
    }

    public bool TryGetRateOverride(string contractId, DateTime eventDate, out double rate)
    {
        rate = 0;
        double totalAdjustmentBps = 0;
        bool hasOverride = false;

        // Find all rate shock events that apply to this date
        foreach (var evt in _events.Where(e => e.EventType == ScenarioEventType.RateShock))
        {
            if (IsEventActive(evt, eventDate))
            {
                totalAdjustmentBps += evt.ValueBps ?? 0;
                hasOverride = true;
            }
        }

        if (hasOverride)
        {
            rate = totalAdjustmentBps / 10000.0; // Convert bps to decimal
        }

        return hasOverride;
    }

    /// <summary>
    /// Get value adjustment percentage for a contract at a given date
    /// </summary>
    public bool TryGetValueAdjustment(string contractId, DateTime eventDate, out double percentageChange)
    {
        percentageChange = 0;
        bool hasAdjustment = false;

        // Find all value adjustment events that apply to this date
        foreach (var evt in _events.Where(e => e.EventType == ScenarioEventType.ValueAdjustment))
        {
            if (IsEventActive(evt, eventDate))
            {
                percentageChange += evt.PercentageChange ?? 0;
                hasAdjustment = true;
            }
        }

        return hasAdjustment;
    }

    /// <summary>
    /// Get all events of a specific type
    /// </summary>
    public IEnumerable<ScenarioEventDefinition> GetEvents(ScenarioEventType? eventType = null)
    {
        return eventType.HasValue 
            ? _events.Where(e => e.EventType == eventType.Value)
            : _events;
    }

    private bool IsEventActive(ScenarioEventDefinition evt, DateTime checkDate)
    {
        // If no dates specified, event is always active
        if (!evt.StartDate.HasValue && !evt.EndDate.HasValue)
            return true;

        // If only start date, active from start date onwards
        if (evt.StartDate.HasValue && !evt.EndDate.HasValue)
            return checkDate >= evt.StartDate.Value;

        // If only end date, active until end date
        if (!evt.StartDate.HasValue && evt.EndDate.HasValue)
            return checkDate <= evt.EndDate.Value;

        // If both dates, active within range (inclusive)
        if (evt.StartDate.HasValue && evt.EndDate.HasValue)
            return checkDate >= evt.StartDate.Value && checkDate <= evt.EndDate.Value;
        
        // Shouldn't reach here, but return false as default
        return false;
    }
}

/// <summary>
/// Scenario event definition
/// </summary>
public sealed record ScenarioEventDefinition
{
    public required ScenarioEventType EventType { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    
    // For rate shocks
    public double? ValueBps { get; init; }
    public string? Curve { get; init; }
    public string? ShockType { get; init; }
    
    // For value adjustments
    public double? PercentageChange { get; init; }
    public string? ContractFilter { get; init; }
    
    // For portfolio operations
    public string? Operation { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Types of scenario events
/// </summary>
public enum ScenarioEventType
{
    RateShock,
    ValueAdjustment,
    PortfolioOperation
}
