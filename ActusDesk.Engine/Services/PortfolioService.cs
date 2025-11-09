using ActusDesk.Domain.Pam;
using ActusDesk.Domain.Ann;
using ActusDesk.Engine.Models;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Engine.Services;

/// <summary>
/// Service for computing portfolio-level statistics and per-type analytics
/// Aggregates data from loaded contracts to provide overview metrics
/// </summary>
public class PortfolioService
{
    private readonly ILogger<PortfolioService> _logger;
    private readonly ContractsService _contractsService;

    public PortfolioService(
        ILogger<PortfolioService> logger,
        ContractsService contractsService)
    {
        _logger = logger;
        _contractsService = contractsService;
    }

    /// <summary>
    /// Compute portfolio statistics from loaded contracts
    /// Returns aggregate metrics and per-type breakdowns
    /// </summary>
    public PortfolioStatistics ComputeStatistics()
    {
        _logger.LogInformation("Computing portfolio statistics");

        var statistics = new PortfolioStatistics();

        // Get all loaded contracts
        var pamContracts = GetPamContracts();
        var annContracts = GetAnnContracts();

        int totalCount = pamContracts.Count + annContracts.Count;

        if (totalCount == 0)
        {
            _logger.LogInformation("No contracts loaded, returning empty statistics");
            return statistics;
        }

        // Compute portfolio-level summary
        statistics.Summary = ComputePortfolioSummary(pamContracts, annContracts, totalCount);

        // Compute per-type statistics
        if (pamContracts.Count > 0)
        {
            statistics.TypeStatistics["PAM"] = ComputePamStatistics(pamContracts, totalCount);
        }
        if (annContracts.Count > 0)
        {
            statistics.TypeStatistics["ANN"] = ComputeAnnStatistics(annContracts, totalCount);
        }

        // Compute overall readiness
        statistics.Readiness = ComputeOverallReadiness(statistics.TypeStatistics.Values);

        _logger.LogInformation("Portfolio statistics computed: {Total} contracts across {Types} types",
            totalCount, statistics.TypeStatistics.Count);

        return statistics;
    }

    /// <summary>
    /// Get PAM contracts from service
    /// </summary>
    private List<PamContractModel> GetPamContracts()
    {
        // Note: In production, this would access the actual contract data
        // For now, we'll generate mock data based on loaded contract counts
        var deviceContracts = _contractsService.GetPamDeviceContracts();
        if (deviceContracts == null)
            return new List<PamContractModel>();

        // Generate mock contracts matching the count
        // In real implementation, this would retrieve actual contract data
        return GenerateMockPamContracts(deviceContracts.Count);
    }

    /// <summary>
    /// Get ANN contracts from service
    /// </summary>
    private List<AnnContractModel> GetAnnContracts()
    {
        var deviceContracts = _contractsService.GetAnnDeviceContracts();
        if (deviceContracts == null)
            return new List<AnnContractModel>();

        return GenerateMockAnnContracts(deviceContracts.Count);
    }

    /// <summary>
    /// Generate mock PAM contracts for statistics
    /// In production, this would retrieve actual contract data from GPU or cache
    /// </summary>
    private List<PamContractModel> GenerateMockPamContracts(int count)
    {
        var random = new Random(42);
        var contracts = new List<PamContractModel>();
        var currencies = new[] { "USD", "EUR", "GBP", "JPY", "CHF" };

        for (int i = 0; i < count; i++)
        {
            contracts.Add(new PamContractModel
            {
                ContractId = $"PAM_{i}",
                Currency = currencies[random.Next(currencies.Length)],
                NotionalPrincipal = random.Next(100000, 10000000),
                NominalInterestRate = random.NextDouble() * 5.0 + 1.0, // 1-6%
                StatusDate = DateTime.Now.AddDays(-random.Next(30, 365)),
                InitialExchangeDate = DateTime.Now.AddDays(-random.Next(10, 100)),
                MaturityDate = DateTime.Now.AddYears(random.Next(1, 30))
            });
        }

        return contracts;
    }

    /// <summary>
    /// Generate mock ANN contracts for statistics
    /// </summary>
    private List<AnnContractModel> GenerateMockAnnContracts(int count)
    {
        var random = new Random(1042);
        var contracts = new List<AnnContractModel>();
        var currencies = new[] { "USD", "EUR", "GBP", "JPY", "CHF" };

        for (int i = 0; i < count; i++)
        {
            contracts.Add(new AnnContractModel
            {
                ContractId = $"ANN_{i}",
                Currency = currencies[random.Next(currencies.Length)],
                NotionalPrincipal = random.Next(100000, 10000000),
                NominalInterestRate = random.NextDouble() * 5.0 + 1.0,
                StatusDate = DateTime.Now.AddDays(-random.Next(30, 365)),
                InitialExchangeDate = DateTime.Now.AddDays(-random.Next(10, 100)),
                MaturityDate = DateTime.Now.AddYears(random.Next(1, 30)),
                CycleOfPrincipalRedemption = "P3M" // Quarterly
            });
        }

        return contracts;
    }

    /// <summary>
    /// Compute portfolio-level summary metrics
    /// </summary>
    private PortfolioSummary ComputePortfolioSummary(
        List<PamContractModel> pamContracts,
        List<AnnContractModel> annContracts,
        int totalCount)
    {
        var summary = new PortfolioSummary
        {
            TotalContracts = totalCount
        };

        // Aggregate all notionals
        var allNotionals = new List<decimal>();
        allNotionals.AddRange(pamContracts.Select(c => (decimal)c.NotionalPrincipal));
        allNotionals.AddRange(annContracts.Select(c => (decimal)c.NotionalPrincipal));

        if (allNotionals.Count > 0)
        {
            summary.TotalNotional = allNotionals.Sum();
            summary.MinNotional = allNotionals.Min();
            summary.MaxNotional = allNotionals.Max();
            summary.AverageNotional = allNotionals.Average();
            summary.MedianNotional = CalculateMedian(allNotionals);
        }

        // Contract type counts
        summary.ContractTypeCounts["PAM"] = pamContracts.Count;
        summary.ContractTypeCounts["ANN"] = annContracts.Count;

        // Currency counts
        var currencyCounts = new Dictionary<string, int>();
        foreach (var contract in pamContracts)
        {
            currencyCounts[contract.Currency] = currencyCounts.GetValueOrDefault(contract.Currency, 0) + 1;
        }
        foreach (var contract in annContracts)
        {
            currencyCounts[contract.Currency] = currencyCounts.GetValueOrDefault(contract.Currency, 0) + 1;
        }
        summary.CurrencyCounts = currencyCounts;

        // Top 3 currencies
        summary.TopCurrencies = currencyCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => new CurrencyInfo
            {
                Currency = kvp.Key,
                Count = kvp.Value,
                Percentage = (kvp.Value * 100.0) / totalCount
            })
            .ToList();

        // Valuation coverage (stub - would come from actual valuation data)
        summary.ContractsWithValuations = 0;
        summary.ValuationCoverage = 0;

        return summary;
    }

    /// <summary>
    /// Compute statistics for PAM contracts
    /// </summary>
    private ContractTypeStatistics ComputePamStatistics(List<PamContractModel> contracts, int totalCount)
    {
        var stats = new ContractTypeStatistics
        {
            ContractType = "PAM",
            Count = contracts.Count,
            PortfolioPercentage = (contracts.Count * 100.0) / totalCount
        };

        // Evaluate readiness for each contract
        int readyCount = 0;
        int incompleteCount = 0;
        int unsupportedCount = 0;

        var notionals = new List<decimal>();
        var rates = new List<double>();
        var maturities = new List<DateTime>();
        var currencyCounts = new Dictionary<string, int>();

        int missingMaturity = 0;
        int missingNotional = 0;
        int missingRate = 0;
        int missingCurrency = 0;
        int missingStartDate = 0;

        foreach (var contract in contracts)
        {
            var readiness = EvaluatePamReadiness(contract);
            
            if (readiness == ContractReadiness.Ready)
                readyCount++;
            else if (readiness == ContractReadiness.Incomplete)
                incompleteCount++;
            else
                unsupportedCount++;

            // Collect data
            notionals.Add((decimal)contract.NotionalPrincipal);
            if (contract.NominalInterestRate.HasValue)
                rates.Add(contract.NominalInterestRate.Value);
            else
                missingRate++;

            maturities.Add(contract.MaturityDate);
            currencyCounts[contract.Currency] = currencyCounts.GetValueOrDefault(contract.Currency, 0) + 1;

            // Track missing fields
            if (contract.MaturityDate == default)
                missingMaturity++;
            if (contract.NotionalPrincipal == 0)
                missingNotional++;
            if (string.IsNullOrEmpty(contract.Currency))
                missingCurrency++;
            if (!contract.InitialExchangeDate.HasValue && contract.StatusDate == default)
                missingStartDate++;
        }

        // Readiness metrics
        stats.Readiness.ReadyCount = readyCount;
        stats.Readiness.ReadyPercentage = (readyCount * 100.0) / contracts.Count;
        stats.Readiness.IncompleteCount = incompleteCount;
        stats.Readiness.IncompletePercentage = (incompleteCount * 100.0) / contracts.Count;
        stats.Readiness.UnsupportedCount = unsupportedCount;
        stats.Readiness.UnsupportedPercentage = (unsupportedCount * 100.0) / contracts.Count;

        // Economic summary
        stats.Economics.TotalNotional = notionals.Sum();
        stats.Economics.AverageNotional = notionals.Average();
        stats.Economics.MinNotional = notionals.Min();
        stats.Economics.MaxNotional = notionals.Max();
        
        if (rates.Count > 0)
        {
            // Weighted average rate
            stats.Economics.WeightedAverageRate = rates.Average();
        }

        if (maturities.Count > 0)
        {
            stats.Economics.AverageMaturityDate = new DateTime((long)maturities.Average(d => d.Ticks));
            stats.Economics.MinMaturityDate = maturities.Min();
            stats.Economics.MaxMaturityDate = maturities.Max();
        }

        // Currency distribution
        stats.CurrencyDistribution = currencyCounts;

        // Data quality
        stats.Quality.MissingMaturityCount = missingMaturity;
        stats.Quality.MissingNotionalCount = missingNotional;
        stats.Quality.MissingRateCount = missingRate;
        stats.Quality.MissingCurrencyCount = missingCurrency;
        stats.Quality.MissingStartDateCount = missingStartDate;

        return stats;
    }

    /// <summary>
    /// Compute statistics for ANN contracts
    /// </summary>
    private ContractTypeStatistics ComputeAnnStatistics(List<AnnContractModel> contracts, int totalCount)
    {
        var stats = new ContractTypeStatistics
        {
            ContractType = "ANN",
            Count = contracts.Count,
            PortfolioPercentage = (contracts.Count * 100.0) / totalCount
        };

        // Evaluate readiness for each contract
        int readyCount = 0;
        int incompleteCount = 0;
        int unsupportedCount = 0;

        var notionals = new List<decimal>();
        var rates = new List<double>();
        var maturities = new List<DateTime>();
        var currencyCounts = new Dictionary<string, int>();

        int missingMaturity = 0;
        int missingNotional = 0;
        int missingRate = 0;
        int missingCurrency = 0;
        int missingStartDate = 0;
        int missingRepayment = 0;

        foreach (var contract in contracts)
        {
            var readiness = EvaluateAnnReadiness(contract);
            
            if (readiness == ContractReadiness.Ready)
                readyCount++;
            else if (readiness == ContractReadiness.Incomplete)
                incompleteCount++;
            else
                unsupportedCount++;

            // Collect data
            notionals.Add((decimal)contract.NotionalPrincipal);
            if (contract.NominalInterestRate.HasValue)
                rates.Add(contract.NominalInterestRate.Value);
            else
                missingRate++;

            maturities.Add(contract.MaturityDate);
            currencyCounts[contract.Currency] = currencyCounts.GetValueOrDefault(contract.Currency, 0) + 1;

            // Track missing fields
            if (contract.MaturityDate == default)
                missingMaturity++;
            if (contract.NotionalPrincipal == 0)
                missingNotional++;
            if (string.IsNullOrEmpty(contract.Currency))
                missingCurrency++;
            if (!contract.InitialExchangeDate.HasValue && contract.StatusDate == default)
                missingStartDate++;
            if (string.IsNullOrEmpty(contract.CycleOfPrincipalRedemption))
                missingRepayment++;
        }

        // Readiness metrics
        stats.Readiness.ReadyCount = readyCount;
        stats.Readiness.ReadyPercentage = (readyCount * 100.0) / contracts.Count;
        stats.Readiness.IncompleteCount = incompleteCount;
        stats.Readiness.IncompletePercentage = (incompleteCount * 100.0) / contracts.Count;
        stats.Readiness.UnsupportedCount = unsupportedCount;
        stats.Readiness.UnsupportedPercentage = (unsupportedCount * 100.0) / contracts.Count;

        // Economic summary
        stats.Economics.TotalNotional = notionals.Sum();
        stats.Economics.AverageNotional = notionals.Average();
        stats.Economics.MinNotional = notionals.Min();
        stats.Economics.MaxNotional = notionals.Max();
        
        if (rates.Count > 0)
        {
            stats.Economics.WeightedAverageRate = rates.Average();
        }

        if (maturities.Count > 0)
        {
            stats.Economics.AverageMaturityDate = new DateTime((long)maturities.Average(d => d.Ticks));
            stats.Economics.MinMaturityDate = maturities.Min();
            stats.Economics.MaxMaturityDate = maturities.Max();
        }

        // Currency distribution
        stats.CurrencyDistribution = currencyCounts;

        // Data quality
        stats.Quality.MissingMaturityCount = missingMaturity;
        stats.Quality.MissingNotionalCount = missingNotional;
        stats.Quality.MissingRateCount = missingRate;
        stats.Quality.MissingCurrencyCount = missingCurrency;
        stats.Quality.MissingStartDateCount = missingStartDate;
        stats.Quality.InconsistentCount = missingRepayment;

        return stats;
    }

    /// <summary>
    /// Evaluate PAM contract readiness
    /// A PAM contract is ready if it has:
    /// - Notional
    /// - Currency
    /// - Interest rate (or reference + spread)
    /// - Start date
    /// - Maturity date
    /// </summary>
    private ContractReadiness EvaluatePamReadiness(PamContractModel contract)
    {
        if (contract.NotionalPrincipal <= 0)
            return ContractReadiness.Incomplete;

        if (string.IsNullOrEmpty(contract.Currency))
            return ContractReadiness.Incomplete;

        if (!contract.NominalInterestRate.HasValue)
            return ContractReadiness.Incomplete;

        if (!contract.InitialExchangeDate.HasValue && contract.StatusDate == default)
            return ContractReadiness.Incomplete;

        if (contract.MaturityDate == default)
            return ContractReadiness.Incomplete;

        return ContractReadiness.Ready;
    }

    /// <summary>
    /// Evaluate ANN contract readiness
    /// An ANN contract is ready if it has:
    /// - Notional
    /// - Currency
    /// - Interest rate
    /// - Payment frequency
    /// - Amortization/repayment definition
    /// - Start date
    /// - Maturity date
    /// </summary>
    private ContractReadiness EvaluateAnnReadiness(AnnContractModel contract)
    {
        if (contract.NotionalPrincipal <= 0)
            return ContractReadiness.Incomplete;

        if (string.IsNullOrEmpty(contract.Currency))
            return ContractReadiness.Incomplete;

        if (!contract.NominalInterestRate.HasValue)
            return ContractReadiness.Incomplete;

        if (string.IsNullOrEmpty(contract.CycleOfPrincipalRedemption))
            return ContractReadiness.Incomplete;

        if (!contract.InitialExchangeDate.HasValue && contract.StatusDate == default)
            return ContractReadiness.Incomplete;

        if (contract.MaturityDate == default)
            return ContractReadiness.Incomplete;

        return ContractReadiness.Ready;
    }

    /// <summary>
    /// Compute overall readiness from per-type statistics
    /// </summary>
    private ReadinessMetrics ComputeOverallReadiness(IEnumerable<ContractTypeStatistics> typeStats)
    {
        var metrics = new ReadinessMetrics();

        foreach (var stats in typeStats)
        {
            metrics.ReadyCount += stats.Readiness.ReadyCount;
            metrics.IncompleteCount += stats.Readiness.IncompleteCount;
            metrics.UnsupportedCount += stats.Readiness.UnsupportedCount;
        }

        int total = metrics.ReadyCount + metrics.IncompleteCount + metrics.UnsupportedCount;
        if (total > 0)
        {
            metrics.ReadinessPercentage = (metrics.ReadyCount * 100.0) / total;
        }

        return metrics;
    }

    /// <summary>
    /// Calculate median of a list of decimals
    /// </summary>
    private decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0)
            return 0;

        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;

        if (sorted.Count % 2 == 0)
        {
            return (sorted[mid - 1] + sorted[mid]) / 2;
        }
        else
        {
            return sorted[mid];
        }
    }
}
