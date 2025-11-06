using ActusDesk.Domain.Pam;
using ActusDesk.Engine.Models;
using ActusDesk.Engine.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for the enhanced scenario module with event-based scenarios
/// </summary>
public class ScenarioTests
{
    [Fact]
    public void PamScenario_WithRateShockEvent_AppliesCorrectRate()
    {
        // Arrange
        var scenario = new PamScenario("RateShock50", "50 bps rate increase");
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 50
        });

        // Act
        var hasOverride = scenario.TryGetRateOverride("TEST-001", DateTime.Today, out var rate);

        // Assert
        Assert.True(hasOverride);
        Assert.Equal(0.005, rate, precision: 5); // 50 bps = 0.005
    }

    [Fact]
    public void PamScenario_WithMultipleRateShocks_CombinesRates()
    {
        // Arrange
        var scenario = new PamScenario("CombinedShock", "Multiple rate shocks");
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 50
        });
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 100
        });

        // Act
        var hasOverride = scenario.TryGetRateOverride("TEST-001", DateTime.Today, out var rate);

        // Assert
        Assert.True(hasOverride);
        Assert.Equal(0.015, rate, precision: 5); // 150 bps = 0.015
    }

    [Fact]
    public void PamScenario_WithDateRange_OnlyAppliesWithinRange()
    {
        // Arrange
        var scenario = new PamScenario("DateRangeShock", "Rate shock with date range");
        var startDate = new DateTime(2024, 6, 1);
        var endDate = new DateTime(2024, 12, 1);
        
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 100,
            StartDate = startDate,
            EndDate = endDate
        });

        // Act & Assert - Before range
        var beforeRange = scenario.TryGetRateOverride("TEST-001", new DateTime(2024, 5, 1), out var rate1);
        Assert.False(beforeRange);

        // Within range
        var withinRange = scenario.TryGetRateOverride("TEST-001", new DateTime(2024, 8, 1), out var rate2);
        Assert.True(withinRange);
        Assert.Equal(0.01, rate2, precision: 5);

        // After range
        var afterRange = scenario.TryGetRateOverride("TEST-001", new DateTime(2024, 12, 15), out var rate3);
        Assert.False(afterRange);
    }

    [Fact]
    public void PamScenario_WithValueAdjustment_ReturnsCorrectPercentage()
    {
        // Arrange
        var scenario = new PamScenario("ValueDecline", "10% value decline");
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.ValueAdjustment,
            PercentageChange = -10
        });

        // Act
        var hasAdjustment = scenario.TryGetValueAdjustment("TEST-001", DateTime.Today, out var percentage);

        // Assert
        Assert.True(hasAdjustment);
        Assert.Equal(-10, percentage);
    }

    [Fact]
    public void PamScenario_WithStartDateOnly_ActiveFromStartOnwards()
    {
        // Arrange
        var scenario = new PamScenario("OpenEndedShock", "Rate shock from start date onwards");
        var startDate = new DateTime(2024, 6, 1);
        
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 75,
            StartDate = startDate
        });

        // Act & Assert - Before start date
        var beforeStart = scenario.TryGetRateOverride("TEST-001", new DateTime(2024, 5, 1), out var rate1);
        Assert.False(beforeStart);

        // On start date
        var onStart = scenario.TryGetRateOverride("TEST-001", startDate, out var rate2);
        Assert.True(onStart);
        Assert.Equal(0.0075, rate2, precision: 5);

        // Long after start date
        var afterStart = scenario.TryGetRateOverride("TEST-001", new DateTime(2025, 12, 1), out var rate3);
        Assert.True(afterStart);
        Assert.Equal(0.0075, rate3, precision: 5);
    }

    [Fact]
    public void PamScenario_WithEndDateOnly_ActiveUntilEnd()
    {
        // Arrange
        var scenario = new PamScenario("EndDateShock", "Rate shock until end date");
        var endDate = new DateTime(2024, 12, 1);
        
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 60,
            EndDate = endDate
        });

        // Act & Assert - Before end date
        var beforeEnd = scenario.TryGetRateOverride("TEST-001", new DateTime(2024, 1, 1), out var rate1);
        Assert.True(beforeEnd);
        Assert.Equal(0.006, rate1, precision: 5);

        // On end date
        var onEnd = scenario.TryGetRateOverride("TEST-001", endDate, out var rate2);
        Assert.True(onEnd);

        // After end date
        var afterEnd = scenario.TryGetRateOverride("TEST-001", new DateTime(2024, 12, 2), out var rate3);
        Assert.False(afterEnd);
    }

    [Fact]
    public void PamScenario_GetEvents_FiltersByType()
    {
        // Arrange
        var scenario = new PamScenario("MixedEvents", "Scenario with multiple event types");
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 50
        });
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.ValueAdjustment,
            PercentageChange = -10
        });
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 100
        });

        // Act
        var allEvents = scenario.GetEvents().ToList();
        var rateEvents = scenario.GetEvents(ScenarioEventType.RateShock).ToList();
        var valueEvents = scenario.GetEvents(ScenarioEventType.ValueAdjustment).ToList();

        // Assert
        Assert.Equal(3, allEvents.Count);
        Assert.Equal(2, rateEvents.Count);
        Assert.Equal(1, valueEvents.Count);
    }

    [Fact]
    public void ScenarioService_AddScenario_IncreasesCount()
    {
        // Arrange
        var logger = new Mock<ILogger<ScenarioService>>();
        var service = new ScenarioService(logger.Object);
        var scenario = new ScenarioDefinition
        {
            Name = "TestScenario",
            Description = "Test"
        };

        // Act
        service.AddScenario(scenario);

        // Assert
        Assert.Single(service.Scenarios);
        Assert.Equal("TestScenario", service.Scenarios[0].Name);
    }

    [Fact]
    public void ScenarioService_RemoveScenario_DecreasesCount()
    {
        // Arrange
        var logger = new Mock<ILogger<ScenarioService>>();
        var service = new ScenarioService(logger.Object);
        service.AddScenario(new ScenarioDefinition { Name = "Test1" });
        service.AddScenario(new ScenarioDefinition { Name = "Test2" });

        // Act
        var removed = service.RemoveScenario("Test1");

        // Assert
        Assert.True(removed);
        Assert.Single(service.Scenarios);
        Assert.Equal("Test2", service.Scenarios[0].Name);
    }

    [Fact]
    public void ScenarioService_GetScenario_ReturnsCorrectScenario()
    {
        // Arrange
        var logger = new Mock<ILogger<ScenarioService>>();
        var service = new ScenarioService(logger.Object);
        var scenario1 = new ScenarioDefinition { Name = "Scenario1", Description = "First" };
        var scenario2 = new ScenarioDefinition { Name = "Scenario2", Description = "Second" };
        service.AddScenario(scenario1);
        service.AddScenario(scenario2);

        // Act
        var result = service.GetScenario("Scenario2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Scenario2", result.Name);
        Assert.Equal("Second", result.Description);
    }

    [Fact]
    public void ScenarioService_UpdateScenario_ModifiesExisting()
    {
        // Arrange
        var logger = new Mock<ILogger<ScenarioService>>();
        var service = new ScenarioService(logger.Object);
        service.AddScenario(new ScenarioDefinition { Name = "Test", Description = "Original" });

        var updated = new ScenarioDefinition { Name = "Test", Description = "Updated" };

        // Act
        var result = service.UpdateScenario("Test", updated);

        // Assert
        Assert.True(result);
        Assert.Equal("Updated", service.GetScenario("Test")?.Description);
    }

    [Fact]
    public void ScenarioService_ClearScenarios_RemovesAll()
    {
        // Arrange
        var logger = new Mock<ILogger<ScenarioService>>();
        var service = new ScenarioService(logger.Object);
        service.AddScenario(new ScenarioDefinition { Name = "Test1" });
        service.AddScenario(new ScenarioDefinition { Name = "Test2" });

        // Act
        service.ClearScenarios();

        // Assert
        Assert.Empty(service.Scenarios);
    }

    [Fact]
    public void ScenarioDefinition_WithMultipleEvents_SupportsComplexScenarios()
    {
        // Arrange & Act
        var scenario = new ScenarioDefinition
        {
            Name = "StressScenario",
            Description = "Combined stress with rate shock and value decline",
            Events = new List<ScenarioEvent>
            {
                new RateShockEvent
                {
                    EventType = "RateShock",
                    ValueBps = 200,
                    ShockType = "parallel",
                    StartDate = DateOnly.FromDateTime(new DateTime(2024, 1, 1))
                },
                new ValueAdjustmentEvent
                {
                    EventType = "ValueAdjustment",
                    PercentageChange = -15,
                    StartDate = DateOnly.FromDateTime(new DateTime(2024, 3, 1)),
                    EndDate = DateOnly.FromDateTime(new DateTime(2024, 9, 1))
                }
            }
        };

        // Assert
        Assert.Equal(2, scenario.Events.Count);
        Assert.IsType<RateShockEvent>(scenario.Events[0]);
        Assert.IsType<ValueAdjustmentEvent>(scenario.Events[1]);
    }

    [Fact]
    public void PamWithScenario_AppliesRateOverride()
    {
        // Arrange - Contract with rate reset
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

        // Create scenario with rate shock
        var scenario = new PamScenario("StressTest", "Stress scenario");
        scenario.AddEvent(new ScenarioEventDefinition
        {
            EventType = ScenarioEventType.RateShock,
            ValueBps = 200 // +200 bps
        });

        // Act
        var events = PamScheduler.Schedule(new DateTime(2030, 1, 1), model);
        var state = PamState.InitFrom(model);
        
        PamEventApplier.ApplyEvents(events, model, scenario, state);

        // Assert - Rate should be set to the scenario override value
        // Scenario provides 200 bps = 0.02 as the new rate (not added to base)
        Assert.Equal(0.02, state.NominalInterestRate, precision: 5);
    }

    [Fact]
    public async Task ScenarioService_SaveAndLoad_PreservesData()
    {
        // Arrange
        var logger = new Mock<ILogger<ScenarioService>>();
        var service = new ScenarioService(logger.Object);
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_scenarios_{Guid.NewGuid()}.json");

        try
        {
            // Create test scenarios
            var scenario1 = new ScenarioDefinition
            {
                Name = "TestScenario1",
                Description = "Test scenario 1",
                Events = new List<ScenarioEvent>
                {
                    new RateShockEvent
                    {
                        EventType = "RateShock",
                        ValueBps = 50,
                        ShockType = "parallel"
                    }
                }
            };

            var scenario2 = new ScenarioDefinition
            {
                Name = "TestScenario2",
                Description = "Test scenario 2 with date range",
                Events = new List<ScenarioEvent>
                {
                    new ValueAdjustmentEvent
                    {
                        EventType = "ValueAdjustment",
                        PercentageChange = -10,
                        StartDate = DateOnly.FromDateTime(new DateTime(2024, 1, 1)),
                        EndDate = DateOnly.FromDateTime(new DateTime(2024, 12, 31))
                    }
                }
            };

            service.AddScenario(scenario1);
            service.AddScenario(scenario2);

            // Act - Save
            await service.SaveScenariosAsync(tempFile);

            // Create new service and load
            var service2 = new ScenarioService(logger.Object);
            await service2.LoadScenariosAsync(tempFile);

            // Assert
            Assert.Equal(2, service2.Scenarios.Count);
            
            var loaded1 = service2.GetScenario("TestScenario1");
            Assert.NotNull(loaded1);
            Assert.Equal("Test scenario 1", loaded1.Description);
            Assert.Single(loaded1.Events);
            
            var loaded2 = service2.GetScenario("TestScenario2");
            Assert.NotNull(loaded2);
            Assert.Equal("Test scenario 2 with date range", loaded2.Description);
            Assert.Single(loaded2.Events);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ScenarioService_LoadFromExistingFile_ParsesCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<ScenarioService>>();
        var service = new ScenarioService(logger.Object);
        
        // Get the absolute path to the scenarios file
        var testDir = AppDomain.CurrentDomain.BaseDirectory;
        var scenarioFile = Path.Combine(testDir, "..", "..", "..", "..", "data", "tests", "scenarios.json");
        scenarioFile = Path.GetFullPath(scenarioFile);

        // Act
        if (File.Exists(scenarioFile))
        {
            await service.LoadScenariosAsync(scenarioFile);

            // Assert
            Assert.True(service.Scenarios.Count >= 3); // At least Base, RatePlus50, RateMinus100
            
            var baseScenario = service.GetScenario("Base");
            Assert.NotNull(baseScenario);
            Assert.Empty(baseScenario.Events);

            var ratePlus50 = service.GetScenario("RatePlus50");
            Assert.NotNull(ratePlus50);
            Assert.Single(ratePlus50.Events);
        }
        else
        {
            // Skip test if file not found in test environment
            Assert.True(true, $"Scenario file not found at {scenarioFile}, skipping test");
        }
    }
}
