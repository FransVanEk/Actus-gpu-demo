using ActusDesk.Domain.Pam;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for PAM (Principal At Maturity) contract scheduling and event application
/// </summary>
public class PamSchedulerTests
{
    [Fact]
    public void PamScheduler_GeneratesBasicEvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-001",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);

        // Assert
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.EventType == PamEventType.IED);
        Assert.Contains(events, e => e.EventType == PamEventType.MD);
    }

    [Fact]
    public void PamScheduler_WithInterestPayments_GeneratesIPEvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-002",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2025, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            CycleOfInterestPayment = "3M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 4, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2026, 1, 1), model);

        // Assert
        var ipEvents = events.Count(e => e.EventType == PamEventType.IP);
        Assert.True(ipEvents >= 3, $"Expected at least 3 IP events, got {ipEvents}");
    }

    [Fact]
    public void PamScheduler_WithCapitalization_GeneratesIPCIEvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-003",
            Currency = "EUR",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2026, 1, 1),
            NotionalPrincipal = 500000,
            NominalInterestRate = 0.04,
            CycleOfInterestPayment = "6M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 7, 1),
            CapitalizationEndDate = new DateTime(2025, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2027, 1, 1), model);

        // Assert
        Assert.Contains(events, e => e.EventType == PamEventType.IPCI);
        
        // All IP events before or at capitalization date should be converted to IPCI
        var ipsBeforeCap = events.Where(e => 
            e.EventType == PamEventType.IP && 
            e.EventDate <= model.CapitalizationEndDate.Value).ToList();
        Assert.Empty(ipsBeforeCap);
    }

    [Fact]
    public void PamScheduler_WithRateReset_GeneratesRREvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-004",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2027, 1, 1),
            NotionalPrincipal = 2000000,
            NominalInterestRate = 0.03,
            CycleOfRateReset = "1Y",
            CycleAnchorDateOfRateReset = new DateTime(2024, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2028, 1, 1), model);

        // Assert
        var rrEvents = events.Count(e => e.EventType == PamEventType.RR);
        Assert.True(rrEvents >= 2, $"Expected at least 2 RR events, got {rrEvents}");
    }

    [Fact]
    public void PamScheduler_WithNextResetRate_GeneratesRRFEvent()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-005",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2027, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.03,
            CycleOfRateReset = "1Y",
            CycleAnchorDateOfRateReset = new DateTime(2024, 1, 1),
            NextResetRate = 0.045
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2028, 1, 1), model);

        // Assert
        Assert.Contains(events, e => e.EventType == PamEventType.RRF);
        
        // The first RR after status date should be converted to RRF
        var firstRRorRRF = events
            .Where(e => (e.EventType == PamEventType.RR || e.EventType == PamEventType.RRF) 
                        && e.EventDate > model.StatusDate)
            .OrderBy(e => e.EventDate)
            .First();
        Assert.Equal(PamEventType.RRF, firstRRorRRF.EventType);
    }

    [Fact]
    public void PamScheduler_WithFeePayment_GeneratesFPEvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-006",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2026, 1, 1),
            NotionalPrincipal = 1000000,
            FeeRate = 0.001,
            CycleOfFee = "1Y",
            CycleAnchorDateOfFee = new DateTime(2024, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2027, 1, 1), model);

        // Assert
        var fpEvents = events.Count(e => e.EventType == PamEventType.FP);
        Assert.True(fpEvents >= 2, $"Expected at least 2 FP events, got {fpEvents}");
    }

    [Fact]
    public void PamScheduler_WithScaling_GeneratesSCEvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-007",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2026, 1, 1),
            NotionalPrincipal = 1000000,
            ScalingEffect = "I",
            CycleOfScalingIndex = "1Y",
            CycleAnchorDateOfScalingIndex = new DateTime(2024, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2027, 1, 1), model);

        // Assert
        var scEvents = events.Count(e => e.EventType == PamEventType.SC);
        Assert.True(scEvents >= 2, $"Expected at least 2 SC events, got {scEvents}");
    }

    [Fact]
    public void PamScheduler_WithPurchaseDate_AddsPRDEvent()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-008",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            PurchaseDate = new DateTime(2024, 6, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);

        // Assert
        Assert.Contains(events, e => e.EventType == PamEventType.PRD);
    }

    [Fact]
    public void PamScheduler_WithTerminationDate_AddsTDAndRemovesLaterEvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-009",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            CycleOfInterestPayment = "1Y",
            CycleAnchorDateOfInterestPayment = new DateTime(2025, 1, 1),
            TerminationDate = new DateTime(2026, 6, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);

        // Assert
        Assert.Contains(events, e => e.EventType == PamEventType.TD);
        Assert.DoesNotContain(events, e => e.EventType == PamEventType.MD);
        Assert.All(events, e => Assert.True(e.EventDate <= model.TerminationDate.Value));
    }

    [Fact]
    public void PamScheduler_RemovesEventsBeforeStatusDate()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-010",
            Currency = "USD",
            StatusDate = new DateTime(2025, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            CycleOfInterestPayment = "1Y",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);

        // Assert
        Assert.All(events, e => Assert.True(e.EventDate >= model.StatusDate));
    }

    [Fact]
    public void PamScheduler_EventsAreSorted()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-011",
            Currency = "USD",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2027, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            CycleOfInterestPayment = "6M",
            CycleAnchorDateOfInterestPayment = new DateTime(2024, 7, 1),
            CycleOfRateReset = "1Y",
            CycleAnchorDateOfRateReset = new DateTime(2025, 1, 1)
        };

        // Act
        var events = PamScheduler.Schedule(new DateTime(2028, 1, 1), model);

        // Assert
        for (int i = 1; i < events.Count; i++)
        {
            Assert.True(events[i].EventDate >= events[i - 1].EventDate,
                $"Events not sorted: {events[i - 1].EventDate} > {events[i].EventDate}");
        }
    }
}
