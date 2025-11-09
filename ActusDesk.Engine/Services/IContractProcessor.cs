using ActusDesk.Gpu;

namespace ActusDesk.Engine.Services;

/// <summary>
/// Interface for processing a specific contract type during valuation
/// Follows Single Responsibility Principle - each processor handles one contract type
/// Follows Open/Closed Principle - new processors can be added without modifying existing code
/// </summary>
public interface IContractProcessor
{
    /// <summary>
    /// The contract type code this processor handles (e.g., "PAM", "ANN")
    /// </summary>
    string ContractType { get; }

    /// <summary>
    /// Check if this processor can handle contracts of the given type
    /// </summary>
    bool CanProcess(string contractType);

    /// <summary>
    /// Process contracts for a specific scenario
    /// </summary>
    /// <param name="gpuContext">GPU context for operations</param>
    /// <param name="scenario">Valuation scenario to apply</param>
    /// <param name="valuationStart">Start date for valuation</param>
    /// <param name="valuationEnd">End date for valuation</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of contract events generated</returns>
    Task<IEnumerable<ContractEvent>> ProcessAsync(
        GpuContext gpuContext,
        ValuationScenario scenario,
        DateTime valuationStart,
        DateTime valuationEnd,
        IProgress<ValuationProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get the number of contracts this processor will handle
    /// </summary>
    int GetContractCount();

    /// <summary>
    /// Check if this processor has contracts to process
    /// </summary>
    bool HasContracts();
}
