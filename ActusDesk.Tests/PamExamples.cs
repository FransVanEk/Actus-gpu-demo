using ActusDesk.Domain.Pam;

namespace ActusDesk.Tests;

/// <summary>
/// Example demonstrating PAM contract usage
/// </summary>
public class PamExamples
{
    [Fact]
    public void Example_SimplePamLoan()
    {
        // Create a simple 5-year loan with quarterly interest payments
        var loan = new PamContractModel
        {
            ContractId = "LOAN-001",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            ContractRole = "RPL", // Borrower
            CycleOfInterestPayment = "3M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 4, 1),
            DayCountConvention = "30E/360"
        };

        // Generate event schedule
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), loan);

        // Verify events
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.EventType == PamEventType.IED);
        Assert.Contains(events, e => e.EventType == PamEventType.MD);
        
        var ipEvents = events.Where(e => e.EventType == PamEventType.IP).ToList();
        Assert.True(ipEvents.Count >= 15, $"Expected at least 15 quarterly interest payments, got {ipEvents.Count}");

        // Initialize state and apply events
        var state = PamState.InitFrom(loan);
        Assert.Equal(1000000, state.NotionalPrincipal); // Positive for borrower
        
        PamEventApplier.ApplyEvents(events, loan, null, state);
        
        // At maturity, loan should be fully paid off
        Assert.Equal(0.0, state.NotionalPrincipal);
    }

    [Fact]
    public void Example_FloatingRateLoanWithScenario()
    {
        // Create a floating rate loan with annual rate resets
        var loan = new PamContractModel
        {
            ContractId = "LOAN-002",
            Currency = "EUR",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2027, 1, 1),
            NotionalPrincipal = 2000000,
            NominalInterestRate = 0.03,
            ContractRole = "RPL",
            CycleOfInterestPayment = "6M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 7, 1),
            CycleOfRateReset = "1Y",
            CycleAnchorDateOfRateReset = new DateTime(2025, 1, 1)
        };

        // Create a stress scenario (e.g., +200bps rate shock)
        var stressScenario = new TestScenario(0.05);

        // Generate and apply events with scenario
        var events = PamScheduler.Schedule(new DateTime(2028, 1, 1), loan);
        var state = PamState.InitFrom(loan);
        
        PamEventApplier.ApplyEvents(events, loan, stressScenario, state);

        // Rate should be updated by scenario
        Assert.Equal(0.05, state.NominalInterestRate);
    }

    [Fact]
    public void Example_BondWithCapitalization()
    {
        // Create a bond that capitalizes interest for the first 2 years
        var bond = new PamContractModel
        {
            ContractId = "BOND-001",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2034, 1, 1),
            NotionalPrincipal = 10000000,
            NominalInterestRate = 0.04,
            ContractRole = "RPA", // Lender/Investor
            CycleOfInterestPayment = "6M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 7, 1),
            CapitalizationEndDate = new DateTime(2026, 1, 1), // Capitalize for 2 years
            DayCountConvention = "ACT/360"
        };

        // Generate events
        var events = PamScheduler.Schedule(new DateTime(2035, 1, 1), bond);

        // All interest events in first 2 years should be IPCI (capitalization)
        var earlyInterestEvents = events
            .Where(e => e.EventDate <= bond.CapitalizationEndDate.Value 
                        && (e.EventType == PamEventType.IP || e.EventType == PamEventType.IPCI))
            .ToList();
        
        Assert.All(earlyInterestEvents, e => Assert.Equal(PamEventType.IPCI, e.EventType));

        // After capitalization period, should have regular IP events
        var lateInterestEvents = events
            .Where(e => e.EventDate > bond.CapitalizationEndDate.Value 
                        && (e.EventType == PamEventType.IP || e.EventType == PamEventType.IPCI))
            .ToList();
        
        Assert.All(lateInterestEvents, e => Assert.Equal(PamEventType.IP, e.EventType));
    }
}
