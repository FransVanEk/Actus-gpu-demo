namespace ActusDesk.Domain;

/// <summary>
/// Base interface for all ACTUS contract terms
/// </summary>
public interface IContractTerms
{
    string Id { get; }
    string Type { get; }
    DateOnly InitialExchangeDate { get; }
    DateOnly? MaturityDate { get; }
    float NotionalPrincipal { get; }
    string Currency { get; }
}

/// <summary>
/// Event in a contract's lifecycle
/// </summary>
public readonly record struct Event(
    DateOnly Date,
    EventType Type,
    float Payoff = 0f,
    string? Currency = null
);

/// <summary>
/// Types of ACTUS events
/// </summary>
public enum EventType : byte
{
    IED = 0,  // Initial Exchange
    PR = 1,   // Principal Redemption
    PP = 2,   // Principal Prepayment
    IP = 3,   // Interest Payment
    FP = 4,   // Fee Payment
    DV = 5,   // Dividend
    RRF = 6,  // Rate Reset Fixing
    RR = 7,   // Rate Reset
    MD = 8,   // Maturity
    TD = 9,   // Termination
    SC = 10,  // Scaling
    IPCL = 11, // Interest Capitalization
    PRD = 12   // Purchase
}

/// <summary>
/// Day count conventions
/// </summary>
public enum DayCountConvention : byte
{
    Act360 = 0,
    Act365F = 1,
    Thirty360 = 2,
    Thirty360E = 3,
    ActActISDA = 4,
    ActActICMA = 5
}

/// <summary>
/// Business day conventions
/// </summary>
public enum BusinessDayConvention : byte
{
    None = 0,
    Following = 1,
    ModifiedFollowing = 2,
    Preceding = 3
}

/// <summary>
/// Calendar types
/// </summary>
public enum CalendarType : byte
{
    None = 0,
    TARGET = 1,
    NewYork = 2,
    London = 3,
    Custom = 255
}
