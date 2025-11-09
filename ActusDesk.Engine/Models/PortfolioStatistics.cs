namespace ActusDesk.Engine.Models;

/// <summary>
/// Overall portfolio statistics and per-type breakdowns
/// Computed from loaded contracts and cached for UI display
/// </summary>
public class PortfolioStatistics
{
    /// <summary>
    /// Portfolio-level summary
    /// </summary>
    public PortfolioSummary Summary { get; set; } = new();

    /// <summary>
    /// Per-contract-type statistics
    /// </summary>
    public Dictionary<string, ContractTypeStatistics> TypeStatistics { get; set; } = new();

    /// <summary>
    /// Overall readiness metrics
    /// </summary>
    public ReadinessMetrics Readiness { get; set; } = new();
}

/// <summary>
/// Portfolio-level summary metrics
/// </summary>
public class PortfolioSummary
{
    /// <summary>
    /// Total number of contracts
    /// </summary>
    public int TotalContracts { get; set; }

    /// <summary>
    /// Total notional amount across all contracts
    /// </summary>
    public decimal TotalNotional { get; set; }

    /// <summary>
    /// Minimum notional among all contracts
    /// </summary>
    public decimal MinNotional { get; set; }

    /// <summary>
    /// Maximum notional among all contracts
    /// </summary>
    public decimal MaxNotional { get; set; }

    /// <summary>
    /// Average notional
    /// </summary>
    public decimal AverageNotional { get; set; }

    /// <summary>
    /// Median notional (if calculable)
    /// </summary>
    public decimal? MedianNotional { get; set; }

    /// <summary>
    /// Distinct contract types and their counts
    /// </summary>
    public Dictionary<string, int> ContractTypeCounts { get; set; } = new();

    /// <summary>
    /// Distinct currencies and their counts
    /// </summary>
    public Dictionary<string, int> CurrencyCounts { get; set; } = new();

    /// <summary>
    /// Top 3 currencies by contract count
    /// </summary>
    public List<CurrencyInfo> TopCurrencies { get; set; } = new();

    /// <summary>
    /// Number of contracts with valuations
    /// </summary>
    public int ContractsWithValuations { get; set; }

    /// <summary>
    /// Valuation coverage percentage
    /// </summary>
    public double ValuationCoverage { get; set; }
}

/// <summary>
/// Currency information with count
/// </summary>
public class CurrencyInfo
{
    public string Currency { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Overall readiness metrics for the portfolio
/// </summary>
public class ReadinessMetrics
{
    /// <summary>
    /// Number of contracts ready for valuation
    /// </summary>
    public int ReadyCount { get; set; }

    /// <summary>
    /// Number of incomplete contracts
    /// </summary>
    public int IncompleteCount { get; set; }

    /// <summary>
    /// Number of unsupported contracts
    /// </summary>
    public int UnsupportedCount { get; set; }

    /// <summary>
    /// Overall readiness percentage
    /// </summary>
    public double ReadinessPercentage { get; set; }
}

/// <summary>
/// Statistics for a specific contract type
/// </summary>
public class ContractTypeStatistics
{
    /// <summary>
    /// Contract type code (PAM, ANN, LAM, etc.)
    /// </summary>
    public string ContractType { get; set; } = "";

    /// <summary>
    /// Number of contracts of this type
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total portfolio
    /// </summary>
    public double PortfolioPercentage { get; set; }

    /// <summary>
    /// Readiness breakdown for this type
    /// </summary>
    public TypeReadiness Readiness { get; set; } = new();

    /// <summary>
    /// Economic summary for this type
    /// </summary>
    public EconomicSummary Economics { get; set; } = new();

    /// <summary>
    /// Currency distribution for this type
    /// </summary>
    public Dictionary<string, int> CurrencyDistribution { get; set; } = new();

    /// <summary>
    /// Valuation snapshot for this type
    /// </summary>
    public ValuationSnapshot? Valuation { get; set; }

    /// <summary>
    /// Data quality indicators
    /// </summary>
    public DataQuality Quality { get; set; } = new();
}

/// <summary>
/// Readiness metrics for a specific contract type
/// </summary>
public class TypeReadiness
{
    /// <summary>
    /// Number ready for valuation
    /// </summary>
    public int ReadyCount { get; set; }

    /// <summary>
    /// Percentage ready
    /// </summary>
    public double ReadyPercentage { get; set; }

    /// <summary>
    /// Number incomplete
    /// </summary>
    public int IncompleteCount { get; set; }

    /// <summary>
    /// Percentage incomplete
    /// </summary>
    public double IncompletePercentage { get; set; }

    /// <summary>
    /// Number unsupported
    /// </summary>
    public int UnsupportedCount { get; set; }

    /// <summary>
    /// Percentage unsupported
    /// </summary>
    public double UnsupportedPercentage { get; set; }
}

/// <summary>
/// Economic summary for a contract type
/// </summary>
public class EconomicSummary
{
    /// <summary>
    /// Total notional for this type
    /// </summary>
    public decimal TotalNotional { get; set; }

    /// <summary>
    /// Average notional
    /// </summary>
    public decimal AverageNotional { get; set; }

    /// <summary>
    /// Minimum notional
    /// </summary>
    public decimal MinNotional { get; set; }

    /// <summary>
    /// Maximum notional
    /// </summary>
    public decimal MaxNotional { get; set; }

    /// <summary>
    /// Weighted average interest rate (if applicable)
    /// </summary>
    public double? WeightedAverageRate { get; set; }

    /// <summary>
    /// Average maturity date
    /// </summary>
    public DateTime? AverageMaturityDate { get; set; }

    /// <summary>
    /// Earliest maturity date
    /// </summary>
    public DateTime? MinMaturityDate { get; set; }

    /// <summary>
    /// Latest maturity date
    /// </summary>
    public DateTime? MaxMaturityDate { get; set; }
}

/// <summary>
/// Valuation snapshot for a contract type
/// </summary>
public class ValuationSnapshot
{
    /// <summary>
    /// Average valuation
    /// </summary>
    public decimal AverageValuation { get; set; }

    /// <summary>
    /// Minimum valuation
    /// </summary>
    public decimal MinValuation { get; set; }

    /// <summary>
    /// Maximum valuation
    /// </summary>
    public decimal MaxValuation { get; set; }

    /// <summary>
    /// Valuation coverage ratio for this type
    /// </summary>
    public double CoverageRatio { get; set; }
}

/// <summary>
/// Data quality indicators for a contract type
/// </summary>
public class DataQuality
{
    /// <summary>
    /// Number of contracts missing maturity dates
    /// </summary>
    public int MissingMaturityCount { get; set; }

    /// <summary>
    /// Number of contracts missing notional values
    /// </summary>
    public int MissingNotionalCount { get; set; }

    /// <summary>
    /// Number of contracts with inconsistent or unsupported entries
    /// </summary>
    public int InconsistentCount { get; set; }

    /// <summary>
    /// Number of contracts missing interest rate
    /// </summary>
    public int MissingRateCount { get; set; }

    /// <summary>
    /// Number of contracts missing currency
    /// </summary>
    public int MissingCurrencyCount { get; set; }

    /// <summary>
    /// Number of contracts missing start date
    /// </summary>
    public int MissingStartDateCount { get; set; }
}

/// <summary>
/// Contract readiness status
/// </summary>
public enum ContractReadiness
{
    /// <summary>
    /// Contract is ready for valuation
    /// </summary>
    Ready,

    /// <summary>
    /// Contract is missing required fields
    /// </summary>
    Incomplete,

    /// <summary>
    /// Contract type is not supported
    /// </summary>
    Unsupported
}
