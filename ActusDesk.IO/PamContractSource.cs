using ActusDesk.Domain.Pam;

namespace ActusDesk.IO;

/// <summary>
/// Interface for providing PAM contracts from any source
/// Decouples the source of contracts from their processing
/// </summary>
public interface IPamContractSource
{
    /// <summary>
    /// Get PAM contracts from this source
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of PAM contract models</returns>
    Task<IEnumerable<PamContractModel>> GetContractsAsync(CancellationToken ct = default);
}

/// <summary>
/// File-based PAM contract source that loads from JSON files
/// </summary>
public class PamFileSource : IPamContractSource
{
    private readonly IEnumerable<string> _filePaths;

    public PamFileSource(string filePath)
    {
        _filePaths = new[] { filePath };
    }

    public PamFileSource(IEnumerable<string> filePaths)
    {
        _filePaths = filePaths;
    }

    public async Task<IEnumerable<PamContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        // Load all files in parallel
        var loadTasks = _filePaths.Select(path => LoadFileAsync(path, ct)).ToList();
        var allTestCases = await Task.WhenAll(loadTasks);

        // Flatten and map all test cases
        return allTestCases
            .SelectMany(testCases => testCases.Values)
            .Select(testCase => ActusPamMapper.MapToPamModel(testCase.Terms));
    }

    private async Task<Dictionary<string, ActusTestCase>> LoadFileAsync(string filePath, CancellationToken ct)
    {
        return await ActusPamMapper.LoadTestCasesAsync(filePath, ct);
    }
}

/// <summary>
/// Mock contract generator for testing and development
/// Generates synthetic PAM contracts with configurable parameters
/// </summary>
public class PamMockSource : IPamContractSource
{
    private readonly int _contractCount;
    private readonly Random _random;

    public PamMockSource(int contractCount, int? seed = null)
    {
        _contractCount = contractCount;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public Task<IEnumerable<PamContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        var contracts = Enumerable.Range(1, _contractCount)
            .Select(i => GenerateMockContract(i))
            .ToList();

        return Task.FromResult<IEnumerable<PamContractModel>>(contracts);
    }

    private PamContractModel GenerateMockContract(int index)
    {
        var statusDate = DateTime.Now.Date;
        var iedDate = statusDate.AddDays(_random.Next(0, 30));
        var maturityYears = _random.Next(1, 10);
        var maturityDate = iedDate.AddYears(maturityYears);

        return new PamContractModel
        {
            ContractId = $"MOCK-PAM-{index:D6}",
            Currency = GetRandomCurrency(),
            StatusDate = statusDate,
            InitialExchangeDate = iedDate,
            MaturityDate = maturityDate,
            NotionalPrincipal = _random.Next(100000, 10000000),
            NominalInterestRate = _random.NextDouble() * 0.1, // 0-10%
            ContractRole = _random.Next(2) == 0 ? "RPA" : "RPL",
            DayCountConvention = GetRandomDayCount(),
            NotionalScalingMultiplier = 1.0,
            InterestScalingMultiplier = 1.0,
            ContractPerformance = "PF"
        };
    }

    private string GetRandomCurrency()
    {
        var currencies = new[] { "USD", "EUR", "GBP", "CHF", "JPY" };
        return currencies[_random.Next(currencies.Length)];
    }

    private string GetRandomDayCount()
    {
        var conventions = new[] { "30E/360", "ACT/360", "ACT/365" };
        return conventions[_random.Next(conventions.Length)];
    }
}

/// <summary>
/// Composite source that combines multiple contract sources
/// </summary>
public class PamCompositeSource : IPamContractSource
{
    private readonly IEnumerable<IPamContractSource> _sources;

    public PamCompositeSource(params IPamContractSource[] sources)
    {
        _sources = sources;
    }

    public PamCompositeSource(IEnumerable<IPamContractSource> sources)
    {
        _sources = sources;
    }

    public async Task<IEnumerable<PamContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        // Load all sources in parallel
        var tasks = _sources.Select(source => source.GetContractsAsync(ct)).ToList();
        var results = await Task.WhenAll(tasks);

        // Flatten all results
        return results.SelectMany(contracts => contracts);
    }
}
