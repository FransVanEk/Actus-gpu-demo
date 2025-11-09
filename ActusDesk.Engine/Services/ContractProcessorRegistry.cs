namespace ActusDesk.Engine.Services;

/// <summary>
/// Registry for contract processors
/// Follows Open/Closed Principle - new processors can be registered without modifying this class
/// Follows Dependency Inversion - depends on IContractProcessor abstraction
/// </summary>
public class ContractProcessorRegistry
{
    private readonly Dictionary<string, IContractProcessor> _processors = new();

    /// <summary>
    /// Register a contract processor
    /// </summary>
    public void Register(IContractProcessor processor)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        var contractType = processor.ContractType.ToUpperInvariant();
        
        if (_processors.ContainsKey(contractType))
        {
            _processors[contractType] = processor;
        }
        else
        {
            _processors.Add(contractType, processor);
        }
    }

    /// <summary>
    /// Get all registered processors
    /// </summary>
    public IEnumerable<IContractProcessor> GetAllProcessors()
    {
        return _processors.Values;
    }

    /// <summary>
    /// Get all processors that have contracts to process
    /// </summary>
    public IEnumerable<IContractProcessor> GetActiveProcessors()
    {
        return _processors.Values.Where(p => p.HasContracts());
    }

    /// <summary>
    /// Get a processor for a specific contract type
    /// </summary>
    public IContractProcessor? GetProcessor(string contractType)
    {
        var key = contractType.ToUpperInvariant();
        return _processors.TryGetValue(key, out var processor) ? processor : null;
    }

    /// <summary>
    /// Check if a processor exists for a contract type
    /// </summary>
    public bool HasProcessor(string contractType)
    {
        return _processors.ContainsKey(contractType.ToUpperInvariant());
    }

    /// <summary>
    /// Get total count of contracts across all processors
    /// </summary>
    public int GetTotalContractCount()
    {
        return _processors.Values.Sum(p => p.GetContractCount());
    }

    /// <summary>
    /// Clear all processors
    /// </summary>
    public void Clear()
    {
        _processors.Clear();
    }
}
