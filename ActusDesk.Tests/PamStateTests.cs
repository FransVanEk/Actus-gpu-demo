using ActusDesk.Domain.Pam;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for PAM state initialization and event application
/// </summary>
public class PamStateTests
{
    [Fact]
    public void PamState_InitFrom_SetsBasicProperties()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-001",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            ContractRole = "RPA"
        };

        // Act
        var state = PamState.InitFrom(model);

        // Assert
        Assert.Equal(-1000000, state.NotionalPrincipal); // RPA = Receiver = -1
        Assert.Equal(0.05, state.NominalInterestRate);
        Assert.Equal(0.0, state.AccruedInterest);
        Assert.Equal(model.StatusDate, state.StatusDate);
    }

    [Fact]
    public void PamState_InitFrom_WithFutureIED_SetsZeroNotional()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-002",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 6, 1), // Future date
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05
        };

        // Act
        var state = PamState.InitFrom(model);

        // Assert
        Assert.Equal(0.0, state.NotionalPrincipal);
        Assert.Equal(0.0, state.NominalInterestRate);
    }

    [Fact]
    public void PamState_InitFrom_WithAccruedInterest_SetsValue()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-003",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            AccruedInterest = 5000
        };

        // Act
        var state = PamState.InitFrom(model);

        // Assert
        Assert.Equal(5000, state.AccruedInterest);
    }

    [Fact]
    public void PamState_InitFrom_WithFeeRate_SetsFeeAccrued()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-004",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            FeeRate = 0.001
        };

        // Act
        var state = PamState.InitFrom(model);

        // Assert
        Assert.Equal(0.001, state.FeeAccrued);
    }

    [Fact]
    public void PamState_InitFrom_WithScalingMultipliers_SetsValues()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-005",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NotionalScalingMultiplier = 1.5,
            InterestScalingMultiplier = 1.2
        };

        // Act
        var state = PamState.InitFrom(model);

        // Assert
        Assert.Equal(1.5, state.NotionalScalingMultiplier);
        Assert.Equal(1.2, state.InterestScalingMultiplier);
    }
}

/// <summary>
/// Tests for PAM event applier
/// </summary>
public class PamEventApplierTests
{
    [Fact]
    public void PamEventApplier_ApplyIED_SetsNotionalAndRate()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-001",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            ContractRole = "RPL" // Payer
        };

        var state = new PamState();
        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = model.InitialExchangeDate.Value,
                EventType = PamEventType.IED,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert
        Assert.Equal(1000000, state.NotionalPrincipal); // RPL = Payer = +1
        Assert.Equal(0.05, state.NominalInterestRate);
    }

    [Fact]
    public void PamEventApplier_ApplyMD_ResetsNotionalAndInterest()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-002",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05
        };

        var state = PamState.InitFrom(model);
        state.AccruedInterest = 10000;

        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = model.MaturityDate,
                EventType = PamEventType.MD,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert
        Assert.Equal(0.0, state.NotionalPrincipal);
        Assert.Equal(0.0, state.AccruedInterest);
    }

    [Fact]
    public void PamEventApplier_ApplyIP_ResetsAccruedInterest()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-003",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05
        };

        var state = PamState.InitFrom(model);
        state.AccruedInterest = 12500; // Some accrued interest

        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = new DateTime(2024, 7, 1),
                EventType = PamEventType.IP,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert
        Assert.Equal(0.0, state.AccruedInterest);
    }

    [Fact]
    public void PamEventApplier_ApplyIPCI_CapitalizesInterest()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-004",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05
        };

        var state = PamState.InitFrom(model);
        state.AccruedInterest = 25000;
        var originalNotional = state.NotionalPrincipal;

        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = new DateTime(2024, 7, 1),
                EventType = PamEventType.IPCI,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert
        Assert.Equal(originalNotional + 25000, state.NotionalPrincipal);
        Assert.Equal(0.0, state.AccruedInterest);
    }

    [Fact]
    public void PamEventApplier_ApplyRR_WithScenario_UsesScenarioRate()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-005",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05
        };

        var state = PamState.InitFrom(model);
        var scenario = new TestScenario(0.06);

        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = new DateTime(2025, 1, 1),
                EventType = PamEventType.RR,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, scenario, state);

        // Assert
        Assert.Equal(0.06, state.NominalInterestRate);
    }

    [Fact]
    public void PamEventApplier_ApplyRRF_WithNextResetRate_UsesNextRate()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-006",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            NextResetRate = 0.045
        };

        var state = PamState.InitFrom(model);

        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = new DateTime(2025, 1, 1),
                EventType = PamEventType.RRF,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert
        Assert.Equal(0.045, state.NominalInterestRate);
    }

    [Fact]
    public void PamEventApplier_ApplyFP_ResetsFeeAccrued()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-007",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            FeeRate = 0.001
        };

        var state = PamState.InitFrom(model);
        state.FeeAccrued = 1000;

        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = new DateTime(2025, 1, 1),
                EventType = PamEventType.FP,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert
        Assert.Equal(0.0, state.FeeAccrued);
    }

    [Fact]
    public void PamEventApplier_ApplyTD_TerminatesContract()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-008",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            TerminationDate = new DateTime(2026, 1, 1)
        };

        var state = PamState.InitFrom(model);
        state.AccruedInterest = 5000;

        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = model.TerminationDate.Value,
                EventType = PamEventType.TD,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert
        Assert.Equal(0.0, state.NotionalPrincipal);
        Assert.Equal(0.0, state.AccruedInterest);
    }

    [Fact]
    public void PamEventApplier_WithPurchaseDate_RemovesPrePurchaseEvents()
    {
        // Arrange
        var model = new PamContractModel
        {
            ContractId = "PAM-009",
            StatusDate = new DateTime(2024, 1, 1),
            InitialExchangeDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2029, 1, 1),
            NotionalPrincipal = 1000000,
            NominalInterestRate = 0.05,
            PurchaseDate = new DateTime(2024, 6, 1)
        };

        var state = new PamState();
        var events = new List<PamEvent>
        {
            new PamEvent
            {
                EventDate = new DateTime(2024, 3, 1),
                EventType = PamEventType.IP,
                ContractId = model.ContractId,
                Currency = model.Currency
            },
            new PamEvent
            {
                EventDate = new DateTime(2024, 6, 1),
                EventType = PamEventType.PRD,
                ContractId = model.ContractId,
                Currency = model.Currency
            },
            new PamEvent
            {
                EventDate = new DateTime(2024, 9, 1),
                EventType = PamEventType.IP,
                ContractId = model.ContractId,
                Currency = model.Currency
            }
        };

        // Act
        PamEventApplier.ApplyEvents(events, model, null, state);

        // Assert - The IP event before purchase should be removed
        // Only PRD and the IP after purchase should remain
        // Since we're testing application, we verify via the events list length change
        // The method modifies the input list
        Assert.Equal(2, events.Count);
    }
}

/// <summary>
/// Test scenario implementation for testing rate overrides
/// </summary>
internal class TestScenario : IPamScenario
{
    private readonly double _rate;

    public TestScenario(double rate)
    {
        _rate = rate;
    }

    public bool TryGetRateOverride(string contractId, DateTime eventDate, out double rate)
    {
        rate = _rate;
        return true;
    }
}
