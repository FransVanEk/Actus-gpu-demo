namespace ActusDesk.Domain;

/// <summary>
/// Generates events for a contract
/// </summary>
public interface IEventGenerator
{
    ReadOnlySpan<Event> Generate(in IContractTerms terms);
}

/// <summary>
/// State machine for contract state transitions
/// </summary>
public interface IStateMachine<TState> where TState : struct
{
    void Apply(ref TState state, in Event ev, IRateProvider rates);
}

/// <summary>
/// Computes payoff for an event
/// </summary>
public interface IPayoff<TState> where TState : struct
{
    float Compute(in TState state, in Event ev, IRateProvider rates);
}

/// <summary>
/// Day count calculator
/// </summary>
public interface IDayCount
{
    double YearFrac(DateOnly start, DateOnly end);
}

/// <summary>
/// Business day adjuster
/// </summary>
public interface IBusinessDayAdjuster
{
    DateOnly Adjust(DateOnly date, BusinessDayConvention convention, CalendarType calendar);
}

/// <summary>
/// Provides interest rates and fixing data
/// </summary>
public interface IRateProvider
{
    float GetRate(string curve, int tenorMonths, DateOnly asOf);
}
