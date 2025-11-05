namespace ActusDesk.Domain.Contracts;

/// <summary>
/// Principal at Maturity (PAM) - simplest ACTUS contract type
/// Characteristics: fixed notional, interest accrues, principal repaid at maturity
/// </summary>
public sealed class PAMTerms : IContractTerms
{
    public required string Id { get; init; }
    public string Type => "PAM";
    public required DateOnly InitialExchangeDate { get; init; }
    public required DateOnly? MaturityDate { get; init; }
    public required float NotionalPrincipal { get; init; }
    public required string Currency { get; init; }
    
    // PAM-specific fields
    public float NominalInterestRate { get; init; }
    public DayCountConvention DayCountConvention { get; init; } = DayCountConvention.Act360;
    public BusinessDayConvention BusinessDayConvention { get; init; } = BusinessDayConvention.Following;
    public CalendarType Calendar { get; init; } = CalendarType.None;
    
    // Payment schedules
    public string? CycleOfInterestPayment { get; init; } // e.g., "P3M" for quarterly
    public DateOnly? CycleAnchorDateOfInterestPayment { get; init; }
}

/// <summary>
/// State for PAM contracts
/// </summary>
public struct PAMState
{
    public float NotionalPrincipal;
    public float AccruedInterest;
    public DateOnly StatusDate;
    public ContractStatus Status;
}

/// <summary>
/// Contract status
/// </summary>
public enum ContractStatus : byte
{
    Performant = 0,
    Delayed = 1,
    Delinquent = 2,
    Default = 3
}
