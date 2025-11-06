using System.Buffers;

namespace ActusDesk.Engine.Models;

/// <summary>
/// Struct-of-Arrays representation of contracts for GPU upload
/// Optimized for coalesced memory access patterns on GPU
/// Lives for entire application lifetime
/// </summary>
public sealed record ContractsSoA : IDisposable
{
    public int Count { get; init; }
    
    // Core contract fields
    public IMemoryOwner<float> Notional { get; init; }
    public IMemoryOwner<float> Rate { get; init; }
    public IMemoryOwner<int> StartYYYYMMDD { get; init; }
    public IMemoryOwner<int> MaturityYYYYMMDD { get; init; }
    public IMemoryOwner<byte> TypeCode { get; init; }
    public IMemoryOwner<uint> CurrencyCode { get; init; }
    public IMemoryOwner<uint> RatingCode { get; init; }
    
    // Event schedule data (variable length per contract)
    public IMemoryOwner<int> EventRowOffsets { get; init; }
    public IMemoryOwner<int> EventKinds { get; init; }
    public IMemoryOwner<int> EventDatesYYYYMMDD { get; init; }

    public ContractsSoA(int count)
    {
        Count = count;
        Notional = MemoryPool<float>.Shared.Rent(count);
        Rate = MemoryPool<float>.Shared.Rent(count);
        StartYYYYMMDD = MemoryPool<int>.Shared.Rent(count);
        MaturityYYYYMMDD = MemoryPool<int>.Shared.Rent(count);
        TypeCode = MemoryPool<byte>.Shared.Rent(count);
        CurrencyCode = MemoryPool<uint>.Shared.Rent(count);
        RatingCode = MemoryPool<uint>.Shared.Rent(count);
        EventRowOffsets = MemoryPool<int>.Shared.Rent(count + 1);
        
        // Estimate: avg 10 events per contract
        EventKinds = MemoryPool<int>.Shared.Rent(count * 10);
        EventDatesYYYYMMDD = MemoryPool<int>.Shared.Rent(count * 10);
    }

    public void Dispose()
    {
        Notional?.Dispose();
        Rate?.Dispose();
        StartYYYYMMDD?.Dispose();
        MaturityYYYYMMDD?.Dispose();
        TypeCode?.Dispose();
        CurrencyCode?.Dispose();
        RatingCode?.Dispose();
        EventRowOffsets?.Dispose();
        EventKinds?.Dispose();
        EventDatesYYYYMMDD?.Dispose();
    }

    /// <summary>
    /// Convert DateOnly to YYYYMMDD integer format
    /// </summary>
    public static int ToYYYYMMDD(DateOnly date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }

    /// <summary>
    /// Convert YYYYMMDD integer to DateOnly
    /// </summary>
    public static DateOnly FromYYYYMMDD(int yyyymmdd)
    {
        int year = yyyymmdd / 10000;
        int month = (yyyymmdd % 10000) / 100;
        int day = yyyymmdd % 100;
        return new DateOnly(year, month, day);
    }
}

/// <summary>
/// Scenario definition for shocks and portfolio operations
/// A scenario is a list of events that can occur on a date or over a period
/// </summary>
public sealed record ScenarioDefinition
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public List<ScenarioEvent> Events { get; init; } = new();
}

/// <summary>
/// Base class for scenario events that can occur on a date or over a period
/// </summary>
public abstract record ScenarioEvent
{
    public required string EventType { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}

/// <summary>
/// Rate shock event (parallel rate bump, spread shock, etc.)
/// </summary>
public sealed record RateShockEvent : ScenarioEvent
{
    public required double ValueBps { get; init; } // Value in basis points
    public string? Curve { get; init; }
    public string? ShockType { get; init; } // "parallel", "twist", "shock_at_tenor", etc.
}

/// <summary>
/// PAM value adjustment event (e.g., for early contract abandonment)
/// </summary>
public sealed record ValueAdjustmentEvent : ScenarioEvent
{
    public required double PercentageChange { get; init; } // -10 for 10% decrease, +5 for 5% increase
    public string? ContractFilter { get; init; } // Optional: filter which contracts are affected
}

/// <summary>
/// Portfolio filter/remap operation event
/// </summary>
public sealed record PortfolioOperationEvent : ScenarioEvent
{
    public required string Operation { get; init; } // "filter", "remap", etc.
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Time grid for cashflow bucketing
/// </summary>
public readonly record struct TimeGrid
{
    public DateOnly Start { get; init; }
    public DateOnly End { get; init; }
    public string Frequency { get; init; } // "M", "Q", "Y"

    public TimeGrid(DateOnly start, DateOnly end, string frequency)
    {
        Start = start;
        End = end;
        Frequency = frequency;
    }
}
