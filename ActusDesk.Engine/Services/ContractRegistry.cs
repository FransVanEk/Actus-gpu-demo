namespace ActusDesk.Engine.Services;

/// <summary>
/// Registry of all supported contract types with their distribution percentages
/// </summary>
public class ContractRegistry
{
    private readonly Dictionary<string, ContractTypeInfo> _contractTypes = new();

    public ContractRegistry()
    {
        // Register default contract types
        RegisterContractType("PAM", "Principal at Maturity", 50.0);
        RegisterContractType("ANN", "Annuity", 50.0);
    }

    public IReadOnlyDictionary<string, ContractTypeInfo> ContractTypes => _contractTypes;

    /// <summary>
    /// Register a new contract type with its percentage
    /// </summary>
    public void RegisterContractType(string code, string description, double percentage)
    {
        if (_contractTypes.ContainsKey(code))
        {
            _contractTypes[code] = new ContractTypeInfo(code, description, percentage);
        }
        else
        {
            _contractTypes.Add(code, new ContractTypeInfo(code, description, percentage));
        }
    }

    /// <summary>
    /// Update the percentage for a contract type
    /// </summary>
    public void UpdatePercentage(string code, double percentage)
    {
        if (_contractTypes.TryGetValue(code, out var info))
        {
            _contractTypes[code] = info with { Percentage = percentage };
        }
    }

    /// <summary>
    /// Get normalized percentages that sum to 100%
    /// </summary>
    public Dictionary<string, double> GetNormalizedPercentages()
    {
        var total = _contractTypes.Values.Sum(ct => ct.Percentage);
        if (total == 0) return new Dictionary<string, double>();

        return _contractTypes.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.Percentage / total) * 100.0
        );
    }

    /// <summary>
    /// Calculate contract counts based on total and percentages
    /// </summary>
    public Dictionary<string, int> CalculateContractCounts(int totalContracts)
    {
        var normalized = GetNormalizedPercentages();
        var counts = new Dictionary<string, int>();
        int allocated = 0;

        // Allocate contracts based on percentages
        var sortedTypes = normalized.OrderByDescending(kvp => kvp.Value).ToList();
        
        for (int i = 0; i < sortedTypes.Count; i++)
        {
            var type = sortedTypes[i];
            int count;
            
            if (i == sortedTypes.Count - 1)
            {
                // Last type gets remainder to ensure total is exact
                count = totalContracts - allocated;
            }
            else
            {
                count = (int)Math.Round(totalContracts * type.Value / 100.0);
                allocated += count;
            }
            
            counts[type.Key] = count;
        }

        return counts;
    }
}

/// <summary>
/// Information about a registered contract type
/// </summary>
public record ContractTypeInfo(string Code, string Description, double Percentage);
