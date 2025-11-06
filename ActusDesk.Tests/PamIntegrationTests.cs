using ActusDesk.Domain.Pam;

namespace ActusDesk.Tests;

/// <summary>
/// Integration tests demonstrating full PAM contract lifecycle
/// </summary>
public class PamIntegrationTests
{
    [Fact]
    public void FullPamLifecycle_WithQuarterlyInterestAndAnnualRateReset()
    {
        // Arrange - Create a typical PAM loan contract
        var model = new PamContractModel
        {
            ContractId = "PAM-LOAN-001",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2026, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            ContractRole = "RPL", // Borrower
            
            // Quarterly interest payments
            CycleOfInterestPayment = "3M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 4, 1),
            
            // Annual rate resets
            CycleOfRateReset = "1Y",
            CycleAnchorDateOfRateReset = new DateTime(2025, 1, 1),
            
            DayCountConvention = "30E/360",
            ContractPerformance = "PF"
        };

        // Act - Schedule events
        var events = PamScheduler.Schedule(new DateTime(2027, 1, 1), model);

        // Assert - Verify event structure
        Assert.NotEmpty(events);
        
        // Should have IED at start
        var iedEvent = events.First(e => e.EventType == PamEventType.IED);
        Assert.Equal(model.InitialExchangeDate.Value, iedEvent.EventDate);
        
        // Should have MD at maturity
        var mdEvent = events.First(e => e.EventType == PamEventType.MD);
        Assert.Equal(model.MaturityDate, mdEvent.EventDate);
        
        // Should have quarterly IP events (approximately 8 over 2 years)
        var ipEvents = events.Where(e => e.EventType == PamEventType.IP).ToList();
        Assert.InRange(ipEvents.Count, 6, 9);
        
        // Should have annual RR events
        var rrEvents = events.Where(e => e.EventType == PamEventType.RR).ToList();
        Assert.InRange(rrEvents.Count, 1, 3);
        
        // Initialize state
        var state = PamState.InitFrom(model);
        Assert.Equal(1000000, state.NotionalPrincipal); // RPL = Payer = positive
        Assert.Equal(0.05, state.NominalInterestRate);
        
        // Apply events
        PamEventApplier.ApplyEvents(events, model, null, state);
        
        // At maturity, notional should be zero
        Assert.Equal(0.0, state.NotionalPrincipal);
        Assert.Equal(model.MaturityDate, state.StatusDate);
    }

    [Fact]
    public void PamWithCapitalization_ConvertsIPtoIPCI()
    {
        // Arrange - Loan with interest capitalization for first year
        var model = new PamContractModel
        {
            ContractId = "PAM-CAP-001",
            Currency = "EUR",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2027, 1, 1),
            NotionalPrincipal = 500000,
            NominalInterestRate = 0.04,
            ContractRole = "RPA", // Lender
            
            // Monthly interest
            CycleOfInterestPayment = "1M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 2, 1),
            
            // Capitalize for first year
            CapitalizationEndDate = new DateTime(2025, 1, 1),
            
            DayCountConvention = "ACT/360"
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2028, 1, 1), model);

        // Assert
        // All interest events before or at cap date should be IPCI
        var eventsBeforeCap = events.Where(e => 
            (e.EventType == PamEventType.IP || e.EventType == PamEventType.IPCI) && 
            e.EventDate <= model.CapitalizationEndDate.Value).ToList();
        
        Assert.All(eventsBeforeCap, e => Assert.Equal(PamEventType.IPCI, e.EventType));
        
        // Events after cap date should be IP
        var eventsAfterCap = events.Where(e => 
            (e.EventType == PamEventType.IP || e.EventType == PamEventType.IPCI) && 
            e.EventDate > model.CapitalizationEndDate.Value).ToList();
        
        Assert.All(eventsAfterCap, e => Assert.Equal(PamEventType.IP, e.EventType));
        
        // Test capitalization increases notional
        var state = PamState.InitFrom(model);
        state.AccruedInterest = 2000; // Simulate accrued interest
        var originalNotional = state.NotionalPrincipal;
        
        var ipciEvent = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = model.CapitalizationEndDate.Value,
                EventType = PamEventType.IPCI,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };
        
        PamEventApplier.ApplyEvents(ipciEvent, model, null, state);
        
        // Notional should increase by accrued interest
        Assert.Equal(originalNotional + 2000, state.NotionalPrincipal);
        Assert.Equal(0.0, state.AccruedInterest);
    }

    [Fact]
    public void PamWithRateResetAndScenario_AppliesScenarioRates()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-SCENARIO-001",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 2000000,
            NominalInterestRate = 0.03,
            ContractRole = "RPL",
            
            CycleOfRateReset = "1Y",
            CycleAnchorDateOfRateReset = new DateTime(2025, 1, 1)
        };

        // Create a stress scenario with +200bps rate shock
        var stressScenario = new TestScenario(0.05); // 5% rate

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);
        var state = PamState.InitFrom(model);
        
        PamEventApplier.ApplyEvents(events, model, stressScenario, state);

        // Assert - Rate should be updated by scenario
        Assert.Equal(0.05, state.NominalInterestRate);
    }

    [Fact]
    public void PamWithNextResetRate_UsesFixedRate()
    {
        // Arrange - Contract with pre-agreed next reset rate
        var model = new PamContractModel
        {
            ContractId = "PAM-FIXED-001",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2027, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.04,
            NextResetRate = 0.045, // Pre-agreed rate for next reset
            
            CycleOfRateReset = "1Y",
            CycleAnchorDateOfRateReset = new DateTime(2025, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2028, 1, 1), model);
        
        // First RR after status should be RRF
        var firstResetEvent = events
            .Where(e => (e.EventType == PamEventType.RR || e.EventType == PamEventType.RRF) 
                        && e.EventDate > model.StatusDate)
            .OrderBy(e => e.EventDate)
            .First();
        
        Assert.Equal(PamEventType.RRF, firstResetEvent.EventType);
        
        // Apply events
        var state = PamState.InitFrom(model);
        PamEventApplier.ApplyEvents(events, model, null, state);
        
        // Rate should be updated to NextResetRate
        Assert.Equal(0.045, state.NominalInterestRate);
    }

    [Fact]
    public void PamWithEarlyTermination_StopsEventsAndReturnsNotional()
    {
        // Arrange - Contract terminated early
        var model = new PamContractModel
        {
            ContractId = "PAM-TERM-001",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            TerminationDate = new DateTime(2026, 6, 1), // Early termination
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            
            CycleOfInterestPayment = "1Y",
            CycleAnchorDateOfInterestPayment = new DateTime(2025, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);

        // Assert
        // All events should be before or at termination date
        Assert.All(events, e => Assert.True(e.EventDate <= model.TerminationDate.Value));
        
        // Should have TD event
        Assert.Contains(events, e => e.EventType == PamEventType.TD);
        
        // Should NOT have MD event (replaced by TD)
        Assert.DoesNotContain(events, e => e.EventType == PamEventType.MD);
        
        // Apply events
        var state = PamState.InitFrom(model);
        PamEventApplier.ApplyEvents(events, model, null, state);
        
        // At termination, contract should be closed
        Assert.Equal(0.0, state.NotionalPrincipal);
        Assert.Equal(0.0, state.AccruedInterest);
    }

    [Fact]
    public void PamWithMultipleFeatures_CombinesCorrectly()
    {
        // Arrange - Complex contract with multiple features
        var model = new PamContractModel
        {
            ContractId = "PAM-COMPLEX-001",
            Currency = "GBP",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 5000000,
            NominalInterestRate = 0.045,
            ContractRole = "RPL",
            
            // Quarterly interest
            CycleOfInterestPayment = "3M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 4, 1),
            
            // Semi-annual rate reset
            CycleOfRateReset = "6M",
            CycleAnchorDateOfRateReset = new DateTime(2024, 7, 1),
            
            // Annual fees
            CycleOfFee = "1Y",
            CycleAnchorDateOfFee = new DateTime(2024, 12, 31),
            FeeRate = 0.001,
            
            // Annual scaling
            ScalingEffect = "I",
            CycleOfScalingIndex = "1Y",
            CycleAnchorDateOfScalingIndex = new DateTime(2024, 6, 1),
            
            NotionalScalingMultiplier = 1.0,
            InterestScalingMultiplier = 1.0
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);

        // Assert - Should have all event types
        Assert.Contains(events, e => e.EventType == PamEventType.IED);
        Assert.Contains(events, e => e.EventType == PamEventType.MD);
        Assert.Contains(events, e => e.EventType == PamEventType.IP);
        Assert.Contains(events, e => e.EventType == PamEventType.RR);
        Assert.Contains(events, e => e.EventType == PamEventType.FP);
        Assert.Contains(events, e => e.EventType == PamEventType.SC);
        
        // Verify counts are reasonable
        var ipCount = events.Count(e => e.EventType == PamEventType.IP);
        var rrCount = events.Count(e => e.EventType == PamEventType.RR);
        var fpCount = events.Count(e => e.EventType == PamEventType.FP);
        var scCount = events.Count(e => e.EventType == PamEventType.SC);
        
        Assert.InRange(ipCount, 15, 21); // ~20 quarterly payments over 5 years
        Assert.InRange(rrCount, 8, 12);  // ~10 semi-annual resets over 5 years
        Assert.InRange(fpCount, 4, 6);   // ~5 annual fees over 5 years
        Assert.InRange(scCount, 4, 6);   // ~5 annual scalings over 5 years
        
        // Events should be sorted
        for (int i = 1; i < events.Count; i++)
        {
            Assert.True(events[i].EventDate >= events[i - 1].EventDate);
        }
    }
}
