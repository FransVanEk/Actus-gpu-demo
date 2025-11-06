namespace ActusDesk.Domain.Contracts;

/// <summary>
/// Annuity (ANN) - amortizing loan with regular payments
/// Characteristics: principal amortizes over time, regular payment includes interest + principal
/// </summary>
public sealed class ANNTerms : IContractTerms
{
    public required string Id { get; init; }
    public string Type => "ANN";
    public required DateOnly InitialExchangeDate { get; init; }
    public required DateOnly? MaturityDate { get; init; }
    public required float NotionalPrincipal { get; init; }
    public required string Currency { get; init; }
    
    // ANN-specific fields
    public float NominalInterestRate { get; init; }
    public DayCountConvention DayCountConvention { get; init; } = DayCountConvention.Act360;
    public BusinessDayConvention BusinessDayConvention { get; init; } = BusinessDayConvention.Following;
    public CalendarType Calendar { get; init; } = CalendarType.None;
    
    // Payment schedules
    public string? CycleOfPrincipalRedemption { get; init; } // e.g., "P1M" for monthly
    public DateOnly? CycleAnchorDateOfPrincipalRedemption { get; init; }
    public string? CycleOfInterestPayment { get; init; }
    public DateOnly? CycleAnchorDateOfInterestPayment { get; init; }
}

/// <summary>
/// State for ANN contracts
/// </summary>
public struct ANNState
{
    public float NotionalPrincipal;
    public float AccruedInterest;
    public DateOnly StatusDate;
    public ContractStatus Status;
}

/// <summary>
/// Linear Amortizer (LAM) - principal reduces linearly
/// </summary>
public sealed class LAMTerms : IContractTerms
{
    public required string Id { get; init; }
    public string Type => "LAM";
    public required DateOnly InitialExchangeDate { get; init; }
    public required DateOnly? MaturityDate { get; init; }
    public required float NotionalPrincipal { get; init; }
    public required string Currency { get; init; }
    
    public float NominalInterestRate { get; init; }
    public DayCountConvention DayCountConvention { get; init; } = DayCountConvention.Act360;
    public BusinessDayConvention BusinessDayConvention { get; init; } = BusinessDayConvention.Following;
    public CalendarType Calendar { get; init; } = CalendarType.None;
    
    public string? CycleOfPrincipalRedemption { get; init; }
    public DateOnly? CycleAnchorDateOfPrincipalRedemption { get; init; }
    public string? CycleOfInterestPayment { get; init; }
    public DateOnly? CycleAnchorDateOfInterestPayment { get; init; }
}

/// <summary>
/// Stock (STK) - equity instrument
/// </summary>
public sealed class STKTerms : IContractTerms
{
    public required string Id { get; init; }
    public string Type => "STK";
    public required DateOnly InitialExchangeDate { get; init; }
    public DateOnly? MaturityDate => null; // Stocks have no maturity
    public required float NotionalPrincipal { get; init; } // Initial price * quantity
    public required string Currency { get; init; }
    
    public float Quantity { get; init; }
    public string? DividendCycle { get; init; }
}

/// <summary>
/// Commodity (COM) - commodity forward/spot
/// </summary>
public sealed class COMTerms : IContractTerms
{
    public required string Id { get; init; }
    public string Type => "COM";
    public required DateOnly InitialExchangeDate { get; init; }
    public required DateOnly? MaturityDate { get; init; }
    public required float NotionalPrincipal { get; init; }
    public required string Currency { get; init; }
    
    public float Quantity { get; init; }
    public string? CommodityType { get; init; }
    public float? DeliveryPrice { get; init; }
}
