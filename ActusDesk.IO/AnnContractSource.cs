using ActusDesk.Domain.Ann;

namespace ActusDesk.IO;

/// <summary>
/// Interface for providing ANN contracts from any source
/// Decouples the source of contracts from their processing
/// </summary>
public interface IAnnContractSource
{
    /// <summary>
    /// Get ANN contracts from this source
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of ANN contract models</returns>
    Task<IEnumerable<AnnContractModel>> GetContractsAsync(CancellationToken ct = default);
}

/// <summary>
/// File-based ANN contract source that loads from JSON files
/// </summary>
public class AnnFileSource : IAnnContractSource
{
    private readonly IEnumerable<string> _filePaths;

    public AnnFileSource(string filePath)
    {
        _filePaths = new[] { filePath };
    }

    public AnnFileSource(IEnumerable<string> filePaths)
    {
        _filePaths = filePaths;
    }

    public async Task<IEnumerable<AnnContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        // Load all files in parallel
        var loadTasks = _filePaths.Select(path => LoadFileAsync(path, ct)).ToList();
        var allTestCases = await Task.WhenAll(loadTasks);

        // Flatten and map all test cases
        return allTestCases
            .SelectMany(testCases => testCases.Values)
            .Select(testCase => ActusAnnMapper.MapToAnnModel(testCase.Terms));
    }

    private async Task<Dictionary<string, ActusTestCase>> LoadFileAsync(string filePath, CancellationToken ct)
    {
        return await ActusAnnMapper.LoadTestCasesAsync(filePath, ct);
    }
}

/// <summary>
/// Mock contract generator for testing and development
/// Generates synthetic ANN contracts with configurable parameters
/// </summary>
public class AnnMockSource : IAnnContractSource
{
    private readonly int _contractCount;
    private readonly Random _random;

    public AnnMockSource(int contractCount, int? seed = null)
    {
        _contractCount = contractCount;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public Task<IEnumerable<AnnContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        var contracts = Enumerable.Range(1, _contractCount)
            .Select(i => GenerateMockContract(i))
            .ToList();

        return Task.FromResult<IEnumerable<AnnContractModel>>(contracts);
    }

    private AnnContractModel GenerateMockContract(int index)
    {
        var statusDate = DateTime.Now.Date;
        var iedDate = statusDate.AddDays(_random.Next(0, 30));
        var maturityYears = _random.Next(1, 30);
        var maturityDate = iedDate.AddYears(maturityYears);

        return new AnnContractModel
        {
            ContractId = $"MOCK-ANN-{index:D6}",
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
            ContractPerformance = "PF",
            CycleAnchorDateOfPrincipalRedemption = iedDate,
            CycleOfPrincipalRedemption = GetRandomCycle(),
            InterestCalculationBase = "NT" // Notional
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

    private string GetRandomCycle()
    {
        var cycles = new[] { "P1M", "P3M", "P6M", "P1Y" };
        return cycles[_random.Next(cycles.Length)];
    }
}

/// <summary>
/// Composite source that combines multiple contract sources
/// </summary>
public class AnnCompositeSource : IAnnContractSource
{
    private readonly IEnumerable<IAnnContractSource> _sources;

    public AnnCompositeSource(params IAnnContractSource[] sources)
    {
        _sources = sources;
    }

    public AnnCompositeSource(IEnumerable<IAnnContractSource> sources)
    {
        _sources = sources;
    }

    public async Task<IEnumerable<AnnContractModel>> GetContractsAsync(CancellationToken ct = default)
    {
        // Load all sources in parallel
        var tasks = _sources.Select(source => source.GetContractsAsync(ct)).ToList();
        var results = await Task.WhenAll(tasks);

        // Flatten all results
        return results.SelectMany(contracts => contracts);
    }
}
