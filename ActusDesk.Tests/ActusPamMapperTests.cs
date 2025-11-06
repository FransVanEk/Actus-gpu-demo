using ActusDesk.Domain.Pam;
using ActusDesk.IO;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for ACTUS PAM test case mapper
/// </summary>
public class ActusPamMapperTests
{
    private const string TestFilePath = "../../../../data/tests/actus-tests-pam.json";

    [Fact]
    public async Task LoadTestCases_ReadsAllTestCases()
    {
        // Act
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);

        // Assert
        Assert.NotEmpty(testCases);
        Assert.True(testCases.Count >= 20, $"Expected at least 20 test cases, got {testCases.Count}");
    }

    [Fact]
    public async Task LoadTestCases_FirstTestCase_HasCorrectStructure()
    {
        // Act
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var firstTest = testCases.Values.First();

        // Assert
        Assert.NotNull(firstTest);
        Assert.NotNull(firstTest.Terms);
        Assert.NotNull(firstTest.Results);
        Assert.NotEmpty(firstTest.Results);
        Assert.Equal("PAM", firstTest.Terms.ContractType);
    }

    [Fact]
    public async Task MapToPamModel_Pam01_MapsCorrectly()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var pam01 = testCases["pam01"];

        // Act
        var model = ActusPamMapper.MapToPamModel(pam01.Terms);

        // Assert
        Assert.Equal("pam01", model.ContractId);
        Assert.Equal("USD", model.Currency);
        Assert.Equal(new DateTime(2012, 12, 30), model.StatusDate);
        Assert.Equal(new DateTime(2013, 1, 1), model.InitialExchangeDate);
        Assert.Equal(new DateTime(2014, 1, 1), model.MaturityDate);
        Assert.Equal(3000, model.NotionalPrincipal);
        Assert.Equal(0.1, model.NominalInterestRate);
        Assert.Equal("RPA", model.ContractRole);
    }

    [Fact]
    public async Task MapToPamModel_Pam01_MapsCycleCorrectly()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var pam01 = testCases["pam01"];

        // Act
        var model = ActusPamMapper.MapToPamModel(pam01.Terms);

        // Assert
        Assert.NotNull(model.CycleOfInterestPayment);
        Assert.Equal(new DateTime(2013, 1, 1), model.CycleAnchorDateOfInterestPayment);
        
        // P1ML0 should normalize to P1M (monthly)
        Assert.Contains("1M", model.CycleOfInterestPayment);
    }

    [Fact]
    public async Task MapToPamModel_AllTestCases_MapWithoutErrors()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);

        // Act & Assert - Should not throw
        foreach (var testCase in testCases.Values)
        {
            var model = ActusPamMapper.MapToPamModel(testCase.Terms);
            Assert.NotNull(model);
            Assert.NotEmpty(model.ContractId);
        }
    }

    [Fact]
    public async Task ActusTestCase_Pam01_HasExpectedResults()
    {
        // Arrange
        var testCases = await ActusPamMapper.LoadTestCasesAsync(TestFilePath);
        var pam01 = testCases["pam01"];

        // Assert
        Assert.NotEmpty(pam01.Results);
        
        // Should have IED event
        var iedEvent = pam01.Results.FirstOrDefault(r => r.EventType == "IED");
        Assert.NotNull(iedEvent);
        Assert.Equal(-3000, iedEvent.Payoff); // Negative for lender (RPA)
        
        // Should have IP events
        var ipEvents = pam01.Results.Where(r => r.EventType == "IP").ToList();
        Assert.NotEmpty(ipEvents);
    }

    [Fact]
    public void MapToPamModel_HandlesMissingOptionalFields()
    {
        // Arrange
        var terms = new ActusTerms
        {
            ContractID = "test",
            StatusDate = "2024-01-01T00:00:00",
            MaturityDate = "2025-01-01T00:00:00"
        };

        // Act
        var model = ActusPamMapper.MapToPamModel(terms);

        // Assert
        Assert.NotNull(model);
        Assert.Equal("test", model.ContractId);
        Assert.Null(model.CycleOfInterestPayment);
        Assert.Null(model.NominalInterestRate);
    }

    [Fact]
    public void MapToPamModel_ParsesDayCountConvention()
    {
        // Arrange  
        var terms = new ActusTerms
        {
            ContractID = "test",
            StatusDate = "2024-01-01T00:00:00",
            MaturityDate = "2025-01-01T00:00:00",
            DayCountConvention = "A365"
        };

        // Act
        var model = ActusPamMapper.MapToPamModel(terms);

        // Assert
        Assert.Equal("ACT/365", model.DayCountConvention);
    }
}
