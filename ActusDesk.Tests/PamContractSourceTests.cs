using ActusDesk.IO;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for PAM contract sources - file, mock, and composite sources
/// </summary>
public class PamContractSourceTests
{
    private const string TestFilePath = "../../../../data/tests/actus-tests-pam.json";

    [Fact]
    public async Task PamFileSource_SingleFile_LoadsContracts()
    {
        // Arrange
        var source = new PamFileSource(TestFilePath);

        // Act
        var contracts = await source.GetContractsAsync();
        var contractsList = contracts.ToList();

        // Assert
        Assert.NotEmpty(contractsList);
        Assert.True(contractsList.Count >= 25, $"Expected at least 25 contracts, got {contractsList.Count}");
        Assert.All(contractsList, c => Assert.NotEmpty(c.ContractId));
    }

    [Fact]
    public async Task PamFileSource_MultipleFiles_CombinesContracts()
    {
        // Arrange
        var filePaths = new[] { TestFilePath, TestFilePath };
        var source = new PamFileSource(filePaths);

        // Act
        var contracts = await source.GetContractsAsync();
        var contractsList = contracts.ToList();

        // Assert
        Assert.True(contractsList.Count >= 50, $"Expected at least 50 contracts from 2 files, got {contractsList.Count}");
    }

    [Fact]
    public async Task PamMockSource_GeneratesContracts()
    {
        // Arrange
        var contractCount = 100;
        var source = new PamMockSource(contractCount, seed: 42);

        // Act
        var contracts = await source.GetContractsAsync();
        var contractsList = contracts.ToList();

        // Assert
        Assert.Equal(contractCount, contractsList.Count);
        Assert.All(contractsList, c => 
        {
            Assert.NotEmpty(c.ContractId);
            Assert.StartsWith("MOCK-PAM-", c.ContractId);
            Assert.True(c.NotionalPrincipal > 0);
            Assert.True(c.MaturityDate > c.StatusDate);
        });
    }

    [Fact]
    public async Task PamMockSource_WithSeed_GeneratesDeterministic()
    {
        // Arrange
        var source1 = new PamMockSource(10, seed: 42);
        var source2 = new PamMockSource(10, seed: 42);

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
        }
    }

    [Fact]
    public async Task PamMockSource_GeneratesVariedContracts()
    {
        // Arrange
        var source = new PamMockSource(100);

        // Act
        var contracts = (await source.GetContractsAsync()).ToList();

        // Assert - Should have variety in generated data
        var currencies = contracts.Select(c => c.Currency).Distinct().ToList();
        var roles = contracts.Select(c => c.ContractRole).Distinct().ToList();
        var dayCountConventions = contracts.Select(c => c.DayCountConvention).Distinct().ToList();

        Assert.True(currencies.Count > 1, "Should have multiple currencies");
        Assert.True(roles.Count > 1, "Should have both RPA and RPL roles");
        Assert.True(dayCountConventions.Count > 1, "Should have multiple day count conventions");
    }

    [Fact]
    public async Task PamCompositeSource_CombinesMultipleSources()
    {
        // Arrange
        var fileSource = new PamFileSource(TestFilePath);
        var mockSource = new PamMockSource(50, seed: 42);
        var composite = new PamCompositeSource(fileSource, mockSource);

        // Act
        var contracts = (await composite.GetContractsAsync()).ToList();

        // Assert
        Assert.True(contracts.Count >= 75, $"Expected at least 75 contracts (25 file + 50 mock), got {contracts.Count}");
        
        // Should have both real and mock contracts
        var mockContracts = contracts.Where(c => c.ContractId.StartsWith("MOCK-")).ToList();
        var fileContracts = contracts.Where(c => !c.ContractId.StartsWith("MOCK-")).ToList();
        
        Assert.NotEmpty(mockContracts);
        Assert.NotEmpty(fileContracts);
    }

    [Fact]
    public async Task PamCompositeSource_EmptySources_ReturnsEmpty()
    {
        // Arrange
        var composite = new PamCompositeSource();

        // Act
        var contracts = (await composite.GetContractsAsync()).ToList();

        // Assert
        Assert.Empty(contracts);
    }

    [Fact]
    public async Task PamCompositeSource_LoadsInParallel()
    {
        // Arrange - Create multiple mock sources
        var sources = Enumerable.Range(1, 5)
            .Select(i => new PamMockSource(20, seed: i))
            .Cast<IPamContractSource>()
            .ToList();
        var composite = new PamCompositeSource(sources);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var contracts = (await composite.GetContractsAsync()).ToList();
        sw.Stop();

        // Assert
        Assert.Equal(100, contracts.Count); // 5 sources × 20 contracts
        
        // Should complete quickly due to parallel loading
        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms, expected < 1000ms");
    }
}
