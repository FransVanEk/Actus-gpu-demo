using ActusDesk.Domain.Pam;
using ActusDesk.Domain.Ann;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Engine.Services;

/// <summary>
/// Base class for contract processors with common functionality
/// </summary>
public abstract class BaseContractProcessor : IContractProcessor
{
    protected readonly ILogger Logger;

    protected BaseContractProcessor(ILogger logger)
    {
        Logger = logger;
    }

    public abstract string ContractType { get; }
    
    public virtual bool CanProcess(string contractType)
    {
        return string.Equals(ContractType, contractType, StringComparison.OrdinalIgnoreCase);
    }

    public abstract Task<IEnumerable<ContractEvent>> ProcessAsync(
        GpuContext gpuContext,
        ValuationScenario scenario,
        DateTime valuationStart,
        DateTime valuationEnd,
        IProgress<ValuationProgress>? progress = null,
        CancellationToken ct = default);

    public abstract int GetContractCount();
    
    public virtual bool HasContracts() => GetContractCount() > 0;

    /// <summary>
    /// Generate events with progress reporting
    /// </summary>
    protected async Task<List<ContractEvent>> GenerateEventsWithProgressAsync(
        int contractCount,
        string contractType,
        DateTime valuationStart,
        DateTime valuationEnd,
        double rateAdjustment,
        Func<string, DateTime, DateTime, double, ContractEvent[]> eventGenerator,
        IProgress<ValuationProgress>? progress,
        CancellationToken ct)
    {
        var events = new List<ContractEvent>();
        int sampleSize = Math.Min(contractCount, 100); // Process up to 100 contracts for demo

        for (int i = 0; i < sampleSize; i++)
        {
            if (i % 10 == 0) // Report progress every 10 contracts
            {
                progress?.Report(new ValuationProgress
                {
                    Stage = $"Processing {contractType} Contracts",
                    ProcessedContracts = i,
                    TotalContracts = contractCount,
                    Message = $"Processing {contractType} contract {i + 1}/{sampleSize}"
                });

                // Allow UI to update
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
            }

            // Generate events for this contract
            var contractId = $"{contractType}_{i}";
            var contractEvents = eventGenerator(contractId, valuationStart, valuationEnd, rateAdjustment);
            events.AddRange(contractEvents);
        }

        Logger.LogInformation("Processed {Count} {Type} contracts", sampleSize, contractType);
        return events;
    }
}

/// <summary>
/// Processor for PAM (Principal at Maturity) contracts
/// Follows Single Responsibility Principle - only handles PAM contracts
/// </summary>
public class PamContractProcessor : BaseContractProcessor
{
    private readonly PamDeviceContracts? _contracts;

    public PamContractProcessor(PamDeviceContracts? contracts, ILogger<PamContractProcessor> logger)
        : base(logger)
    {
        _contracts = contracts;
    }

    public override string ContractType => "PAM";

    public override int GetContractCount() => _contracts?.Count ?? 0;

    public override async Task<IEnumerable<ContractEvent>> ProcessAsync(
        GpuContext gpuContext,
        ValuationScenario scenario,
        DateTime valuationStart,
        DateTime valuationEnd,
        IProgress<ValuationProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (_contracts == null || _contracts.Count == 0)
        {
            return Array.Empty<ContractEvent>();
        }

        double rateAdjustment = scenario.RateBumpBps / 10000.0;

        return await GenerateEventsWithProgressAsync(
            _contracts.Count,
            ContractType,
            valuationStart,
            valuationEnd,
            rateAdjustment,
            GeneratePamEvents,
            progress,
            ct);
    }

    private ContractEvent[] GeneratePamEvents(
        string contractId,
        DateTime valuationStart,
        DateTime valuationEnd,
        double rateAdjustment)
    {
        var events = new List<ContractEvent>();
        var random = new Random(contractId.GetHashCode());
        double notional = 100000 + random.NextDouble() * 900000; // 100k to 1M
        double rate = 0.05 + rateAdjustment; // 5% base rate + scenario adjustment

        // IED - Initial Exchange
        var iedDate = DateOnly.FromDateTime(valuationStart);
        events.Add(new ContractEvent
        {
            ContractId = contractId,
            ContractType = "PAM",
            EventType = "IED",
            EventDate = iedDate,
            Payoff = -(decimal)notional,
            PresentValue = -(decimal)notional,
            Currency = "USD"
        });

        // IP - Interest Payments (quarterly)
        var currentDate = valuationStart.AddMonths(3);
        while (currentDate <= valuationEnd)
        {
            double interest = notional * rate * 0.25; // Quarterly interest
            double discountFactor = Math.Pow(1 + rate, -((currentDate - valuationStart).TotalDays / 365.0));

            events.Add(new ContractEvent
            {
                ContractId = contractId,
                ContractType = "PAM",
                EventType = "IP",
                EventDate = DateOnly.FromDateTime(currentDate),
                Payoff = (decimal)interest,
                PresentValue = (decimal)(interest * discountFactor),
                Currency = "USD"
            });

            currentDate = currentDate.AddMonths(3);
        }

        // MD - Maturity (principal repayment)
        var mdDate = DateOnly.FromDateTime(valuationEnd);
        double mdDiscountFactor = Math.Pow(1 + rate, -((valuationEnd - valuationStart).TotalDays / 365.0));
        events.Add(new ContractEvent
        {
            ContractId = contractId,
            ContractType = "PAM",
            EventType = "MD",
            EventDate = mdDate,
            Payoff = (decimal)notional,
            PresentValue = (decimal)(notional * mdDiscountFactor),
            Currency = "USD"
        });

        return events.ToArray();
    }
}

/// <summary>
/// Processor for ANN (Annuity) contracts
/// Follows Single Responsibility Principle - only handles ANN contracts
/// </summary>
public class AnnContractProcessor : BaseContractProcessor
{
    private readonly AnnDeviceContracts? _contracts;

    public AnnContractProcessor(AnnDeviceContracts? contracts, ILogger<AnnContractProcessor> logger)
        : base(logger)
    {
        _contracts = contracts;
    }

    public override string ContractType => "ANN";

    public override int GetContractCount() => _contracts?.Count ?? 0;

    public override async Task<IEnumerable<ContractEvent>> ProcessAsync(
        GpuContext gpuContext,
        ValuationScenario scenario,
        DateTime valuationStart,
        DateTime valuationEnd,
        IProgress<ValuationProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (_contracts == null || _contracts.Count == 0)
        {
            return Array.Empty<ContractEvent>();
        }

        double rateAdjustment = scenario.RateBumpBps / 10000.0;

        return await GenerateEventsWithProgressAsync(
            _contracts.Count,
            ContractType,
            valuationStart,
            valuationEnd,
            rateAdjustment,
            GenerateAnnEvents,
            progress,
            ct);
    }

    private ContractEvent[] GenerateAnnEvents(
        string contractId,
        DateTime valuationStart,
        DateTime valuationEnd,
        double rateAdjustment)
    {
        var events = new List<ContractEvent>();
        var random = new Random(contractId.GetHashCode());
        double notional = 50000 + random.NextDouble() * 450000; // 50k to 500k
        double rate = 0.04 + rateAdjustment; // 4% base rate + scenario adjustment
        int periods = 40; // 10 years quarterly

        // Calculate annuity payment
        double payment = notional * (rate / 4) / (1 - Math.Pow(1 + rate / 4, -periods));

        // IED - Initial Exchange
        var iedDate = DateOnly.FromDateTime(valuationStart);
        events.Add(new ContractEvent
        {
            ContractId = contractId,
            ContractType = "ANN",
            EventType = "IED",
            EventDate = iedDate,
            Payoff = -(decimal)notional,
            PresentValue = -(decimal)notional,
            Currency = "USD"
        });

        // PR - Principal Redemption and IP - Interest Payment (combined in annuity)
        var currentDate = valuationStart.AddMonths(3);
        double remainingPrincipal = notional;
        int period = 1;

        while (currentDate <= valuationEnd && period <= periods)
        {
            double interest = remainingPrincipal * (rate / 4);
            double principal = payment - interest;
            remainingPrincipal -= principal;

            double discountFactor = Math.Pow(1 + rate, -((currentDate - valuationStart).TotalDays / 365.0));

            // Interest payment event
            events.Add(new ContractEvent
            {
                ContractId = contractId,
                ContractType = "ANN",
                EventType = "IP",
                EventDate = DateOnly.FromDateTime(currentDate),
                Payoff = (decimal)interest,
                PresentValue = (decimal)(interest * discountFactor),
                Currency = "USD"
            });

            // Principal redemption event
            events.Add(new ContractEvent
            {
                ContractId = contractId,
                ContractType = "ANN",
                EventType = "PR",
                EventDate = DateOnly.FromDateTime(currentDate),
                Payoff = (decimal)principal,
                PresentValue = (decimal)(principal * discountFactor),
                Currency = "USD"
            });

            currentDate = currentDate.AddMonths(3);
            period++;
        }

        return events.ToArray();
    }
}
