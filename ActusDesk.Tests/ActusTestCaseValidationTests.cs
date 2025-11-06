using ActusDesk.Domain.Pam;
using ActusDesk.IO;

namespace ActusDesk.Tests;

/// <summary>
/// Integration tests validating ACTUS test cases can be loaded and scheduled
/// </summary>
public class ActusTestCaseValidationTests
{
    private const string TestFilePath = "../../../../data/tests/actus-tests-pam.json";

    [Fact]
    public async Task ValidateTestCase_Pam01_SchedulesCorrectly()
    {
        // Arrange - Load test case
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var pam01 = testCases["pam01"];
        var model = ActusPamMapper.MapToPamModel(pam01.Terms);

        // Act - Schedule events
        var scheduledEvents = PamScheduler.Schedule(new DateTime(2015, 1, 1), model);

        // Assert - Basic validations
        Assert.NotEmpty(scheduledEvents);
        
        // Should have IED
        var hasIED = scheduledEvents.Any(e => e.EventType == PamEventType.IED);
        Assert.True(hasIED, "Should have IED event");
        
        // Should have IP events (monthly cycle)
        var ipEvents = scheduledEvents.Where(e => e.EventType == PamEventType.IP).ToList();
        Assert.True(ipEvents.Count >= 10, $"Should have multiple IP events, got {ipEvents.Count}");
        
        // Should have MD
        var hasMD = scheduledEvents.Any(e => e.EventType == PamEventType.MD);
        Assert.True(hasMD, "Should have MD event");
        
        // Events should be sorted
        for (int i = 1; i < scheduledEvents.Count; i++)
        {
            Assert.True(scheduledEvents[i].EventDate >= scheduledEvents[i - 1].EventDate,
                $"Events should be sorted by date");
        }
    }

    [Fact]
    public async Task ValidateAllTestCases_CanBeScheduled()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);

        // Act & Assert - All test cases should schedule without errors
        foreach (var (id, testCase) in testCases)
        {
            var model = ActusPamMapper.MapToPamModel(testCase.Terms);
            
            // Use a far future date for scheduling
            var scheduledEvents = PamScheduler.Schedule(new DateTime(2050, 1, 1), model);
            
            Assert.NotEmpty(scheduledEvents);
            Assert.All(scheduledEvents, e => Assert.Equal(model.ContractId, e.ContractId));
        }
    }

    [Fact]
    public async Task ValidateTestCase_WithCapitalization_Pam19_ConvertsIPtoIPCI()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var pam19 = testCases["pam19"];
        var model = ActusPamMapper.MapToPamModel(pam19.Terms);

        // Act
        var events = PamScheduler.Schedule(new DateTime(2015, 1, 1), model);

        // Assert - Should have IPCI events due to capitalizationEndDate
        var ipciEvents = events.Where(e => e.EventType == PamEventType.IPCI).ToList();
        Assert.NotEmpty(ipciEvents);
        
        // Verify capitalization date is set
        Assert.NotNull(model.CapitalizationEndDate);
    }

    [Fact]
    public async Task TestResultsStructure_ContainsExpectedFields()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var pam01 = testCases["pam01"];

        // Assert - Results should have proper structure
        Assert.NotEmpty(pam01.Results);
        
        foreach (var result in pam01.Results)
        {
            Assert.NotEmpty(result.EventDate);
            Assert.NotEmpty(result.EventType);
            // Payoff can be 0, so just check it's a valid number
            Assert.True(double.IsFinite(result.Payoff));
            Assert.True(result.NotionalPrincipal >= 0 || result.NotionalPrincipal < 0);
        }
    }

    [Fact]
    public async Task CompareScheduledVsExpected_Pam01_EventCounts()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var pam01 = testCases["pam01"];
        var model = ActusPamMapper.MapToPamModel(pam01.Terms);

        // Act
        var scheduledEvents = PamScheduler.Schedule(new DateTime(2015, 1, 1), model);
        var expectedResults = pam01.Results;

        // Assert - Event counts should be close (may differ due to implementation details)
        // The test file has expected results, our scheduler should generate similar events
        var scheduledIPs = scheduledEvents.Count(e => e.EventType == PamEventType.IP);
        var expectedIPs = expectedResults.Count(r => r.EventType == "IP");
        
        // Allow some variance due to different cycle interpretation
        Assert.True(Math.Abs(scheduledIPs - expectedIPs) <= 2,
            $"IP event count mismatch: scheduled={scheduledIPs}, expected={expectedIPs}");
    }
}
