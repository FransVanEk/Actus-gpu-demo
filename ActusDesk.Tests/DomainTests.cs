using ActusDesk.Domain;
using ActusDesk.Domain.Contracts;
using ActusDesk.Domain.EventGeneration;
using ActusDesk.Domain.DayCounts;
using ActusDesk.Domain.Rates;

namespace ActusDesk.Tests;

public class EventGenerationTests
{
    [Fact]
    public void PAMEventGenerator_GeneratesIEDAndMD()
    {
        // Arrange
        var generator = new PAMEventGenerator();
        var terms = new PAMTerms
        {
            Id = "TEST-001",
            InitialExchangeDate = new DateOnly(2024, 1, 1),
            MaturityDate = new DateOnly(2029, 1, 1),
            NotionalPrincipal = 1000000f,
            Currency = "USD",
            NominalInterestRate = 0.05f
        };

        // Act
        var events = generator.Generate(terms);

        // Assert
        Assert.True(events.Length >= 2); // At least IED and MD
        Assert.Equal(EventType.IED, events[0].Type);
        Assert.Equal(EventType.MD, events[^1].Type);
    }

    [Fact]
    public void PAMEventGenerator_WithInterestPayments_GeneratesIPEvents()
    {
        // Arrange
        var generator = new PAMEventGenerator();
        var terms = new PAMTerms
        {
            Id = "TEST-002",
            InitialExchangeDate = new DateOnly(2024, 1, 1),
            MaturityDate = new DateOnly(2025, 1, 1),
            NotionalPrincipal = 1000000f,
            Currency = "USD",
            NominalInterestRate = 0.05f,
            CycleOfInterestPayment = "P3M", // Quarterly
            CycleAnchorDateOfInterestPayment = new DateOnly(2024, 4, 1)
        };

        // Act
        var events = generator.Generate(terms);

        // Assert
        var ipEvents = events.ToArray().Count(e => e.Type == EventType.IP);
        Assert.True(ipEvents >= 3); // At least 3 quarterly payments
    }
}

public class ValuationTests
{
    [Fact]
    public void DeterministicValuator_CalculatesPV()
    {
        // Arrange
        var dayCount = new Act360DayCount();
        var rateProvider = new ConstantRateProvider(0.03f);
        var valuator = new DeterministicValuator(dayCount, rateProvider);
        
        var terms = new PAMTerms
        {
            Id = "TEST-003",
            InitialExchangeDate = new DateOnly(2024, 1, 1),
            MaturityDate = new DateOnly(2029, 1, 1),
            NotionalPrincipal = 1000000f,
            Currency = "USD",
            NominalInterestRate = 0.05f
        };

        // Act
        float pv = valuator.CalculatePV(terms, new DateOnly(2024, 1, 1));

        // Assert
        Assert.True(pv > 0);
        Assert.True(pv > terms.NotionalPrincipal); // Should be greater due to interest
    }
}
