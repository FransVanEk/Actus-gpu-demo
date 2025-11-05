using ActusDesk.Domain.Contracts;

namespace ActusDesk.Domain.EventGeneration;

/// <summary>
/// Event generator for PAM (Principal at Maturity) contracts
/// </summary>
public sealed class PAMEventGenerator : IEventGenerator
{
    public ReadOnlySpan<Event> Generate(in IContractTerms terms)
    {
        if (terms is not PAMTerms pamTerms)
        {
            throw new ArgumentException("Terms must be PAMTerms", nameof(terms));
        }

        var events = new List<Event>();

        // IED - Initial Exchange Date
        events.Add(new Event(
            pamTerms.InitialExchangeDate,
            EventType.IED,
            -pamTerms.NotionalPrincipal, // Cash outflow
            pamTerms.Currency
        ));

        // IP - Interest Payment events
        if (!string.IsNullOrEmpty(pamTerms.CycleOfInterestPayment) && 
            pamTerms.CycleAnchorDateOfInterestPayment.HasValue)
        {
            var ipDate = pamTerms.CycleAnchorDateOfInterestPayment.Value;
            var maturity = pamTerms.MaturityDate ?? pamTerms.InitialExchangeDate.AddYears(5);
            
            while (ipDate <= maturity)
            {
                if (ipDate > pamTerms.InitialExchangeDate)
                {
                    events.Add(new Event(
                        ipDate,
                        EventType.IP,
                        0, // Payoff calculated later
                        pamTerms.Currency
                    ));
                }
                ipDate = AddCycle(ipDate, pamTerms.CycleOfInterestPayment);
            }
        }

        // MD - Maturity Date (principal redemption)
        if (pamTerms.MaturityDate.HasValue)
        {
            events.Add(new Event(
                pamTerms.MaturityDate.Value,
                EventType.MD,
                pamTerms.NotionalPrincipal, // Cash inflow
                pamTerms.Currency
            ));
        }

        // Sort events by date
        events.Sort((a, b) => a.Date.CompareTo(b.Date));

        return events.ToArray().AsSpan();
    }

    private static DateOnly AddCycle(DateOnly date, string cycle)
    {
        // Parse ISO 8601 duration format (simplified)
        // P3M = 3 months, P6M = 6 months, P1Y = 1 year
        if (cycle.StartsWith("P") && cycle.EndsWith("M"))
        {
            int months = int.Parse(cycle[1..^1]);
            return date.AddMonths(months);
        }
        if (cycle.StartsWith("P") && cycle.EndsWith("Y"))
        {
            int years = int.Parse(cycle[1..^1]);
            return date.AddYears(years);
        }
        
        throw new ArgumentException($"Unsupported cycle format: {cycle}");
    }
}

/// <summary>
/// Simple deterministic valuation engine for CPU reference
/// Used for testing and as correctness oracle for GPU results
/// </summary>
public sealed class DeterministicValuator
{
    private readonly IDayCount _dayCount;
    private readonly IRateProvider _rateProvider;

    public DeterministicValuator(IDayCount dayCount, IRateProvider rateProvider)
    {
        _dayCount = dayCount;
        _rateProvider = rateProvider;
    }

    public float CalculatePV(IContractTerms terms, DateOnly valuationDate)
    {
        if (terms is PAMTerms pamTerms)
        {
            return CalculatePAM_PV(pamTerms, valuationDate);
        }

        throw new NotSupportedException($"Contract type {terms.Type} not supported");
    }

    private float CalculatePAM_PV(PAMTerms terms, DateOnly valuationDate)
    {
        float pv = 0f;

        // Simple PV: discount future cashflows
        var maturity = terms.MaturityDate ?? terms.InitialExchangeDate.AddYears(5);
        double years = _dayCount.YearFrac(valuationDate, maturity);
        
        float discountRate = _rateProvider.GetRate("USD", (int)(years * 12), valuationDate);
        float discountFactor = (float)Math.Pow(1.0 + discountRate, -years);

        // Future value = notional + accrued interest
        float totalYears = (float)_dayCount.YearFrac(terms.InitialExchangeDate, maturity);
        float futureValue = terms.NotionalPrincipal * (1 + terms.NominalInterestRate * totalYears);

        pv = futureValue * discountFactor;

        return pv;
    }
}
