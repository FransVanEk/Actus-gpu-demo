using ActusDesk.IO;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for ANN contract sources - file, mock, and composite sources
/// </summary>
public class AnnContractSourceTests
{
    [Fact]
    public async Task AnnMockSource_GeneratesContracts()
    {
        // Arrange
        var contractCount = 100;
        var source = new AnnMockSource(contractCount, seed: 42);

        // Act
        var contracts = await source.GetContractsAsync();
        var contractsList = contracts.ToList();

        // Assert
        Assert.Equal(contractCount, contractsList.Count);
        Assert.All(contractsList, c => 
        {
            Assert.NotEmpty(c.ContractId);
            Assert.StartsWith("MOCK-ANN-", c.ContractId);
            Assert.True(c.NotionalPrincipal > 0);
            Assert.True(c.MaturityDate > c.StatusDate);
            Assert.NotNull(c.CycleOfPrincipalRedemption);
            Assert.NotNull(c.CycleAnchorDateOfPrincipalRedemption);
        });
    }

    [Fact]
    public async Task AnnMockSource_WithSeed_GeneratesDeterministic()
    {
        // Arrange
        var source1 = new AnnMockSource(10, seed: 42);
        var source2 = new AnnMockSource(10, seed: 42);

        // Act
        var contracts1 = (await source1.GetContractsAsync()).ToList();
        var contracts2 = (await source2.GetContractsAsync()).ToList();

        // Assert - Same seed should produce same contracts
        Assert.Equal(contracts1.Count, contracts2.Count);
        for (int i = 0; i < contracts1.Count; i++)
        {
            Assert.Equal(contracts1[i].NotionalPrincipal, contracts2[i].NotionalPrincipal);
            Assert.Equal(contracts1[i].NominalInterestRate, contracts2[i].NominalInterestRate);
            Assert.Equal(contracts1[i].Currency, contracts2[i].Currency);
            Assert.Equal(contracts1[i].CycleOfPrincipalRedemption, contracts2[i].CycleOfPrincipalRedemption);
        }
    }

    [Fact]
    public async Task AnnMockSource_GeneratesVariedContracts()
    {
        // Arrange
        var source = new AnnMockSource(100);

        // Act
        var contracts = (await source.GetContractsAsync()).ToList();

        // Assert - Should have variety in generated data
        var currencies = contracts.Select(c => c.Currency).Distinct().ToList();
        var roles = contracts.Select(c => c.ContractRole).Distinct().ToList();
        var dayCountConventions = contracts.Select(c => c.DayCountConvention).Distinct().ToList();
        var cycles = contracts.Select(c => c.CycleOfPrincipalRedemption).Distinct().ToList();

        Assert.True(currencies.Count > 1, "Should have multiple currencies");
        Assert.True(roles.Count > 1, "Should have both RPA and RPL roles");
        Assert.True(dayCountConventions.Count > 1, "Should have multiple day count conventions");
        Assert.True(cycles.Count > 1, "Should have multiple redemption cycles");
    }

    [Fact]
    public async Task AnnCompositeSource_CombinesMultipleSources()
    {
        // Arrange
        var mockSource1 = new AnnMockSource(30, seed: 42);
        var mockSource2 = new AnnMockSource(50, seed: 100);
        var composite = new AnnCompositeSource(mockSource1, mockSource2);

        // Act
        var contracts = (await composite.GetContractsAsync()).ToList();

        // Assert
        Assert.Equal(80, contracts.Count); // 30 + 50
        Assert.All(contracts, c => Assert.StartsWith("MOCK-ANN-", c.ContractId));
    }

    [Fact]
    public async Task AnnCompositeSource_EmptySources_ReturnsEmpty()
    {
        // Arrange
        var composite = new AnnCompositeSource();

        // Act
        var contracts = (await composite.GetContractsAsync()).ToList();

        // Assert
        Assert.Empty(contracts);
    }

    [Fact]
    public async Task AnnCompositeSource_LoadsInParallel()
    {
        // Arrange - Create multiple mock sources
        var sources = Enumerable.Range(1, 5)
            .Select(i => new AnnMockSource(20, seed: i))
            .Cast<IAnnContractSource>()
            .ToList();
        var composite = new AnnCompositeSource(sources);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var contracts = (await composite.GetContractsAsync()).ToList();
        sw.Stop();

        // Assert
        Assert.Equal(100, contracts.Count); // 5 sources × 20 contracts
        
        // Should complete quickly due to parallel loading
        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public async Task AnnMockSource_GeneratesAnnSpecificFields()
    {
        // Arrange
        var source = new AnnMockSource(10, seed: 42);

        // Act
        var contracts = (await source.GetContractsAsync()).ToList();

        // Assert - Verify ANN-specific fields are populated
        Assert.All(contracts, c => 
        {
            Assert.NotNull(c.CycleOfPrincipalRedemption);
            Assert.NotNull(c.CycleAnchorDateOfPrincipalRedemption);
            Assert.Equal("NT", c.InterestCalculationBase);
            Assert.Equal(1.0, c.NotionalScalingMultiplier);
            Assert.Equal(1.0, c.InterestScalingMultiplier);
        });
    }
}
