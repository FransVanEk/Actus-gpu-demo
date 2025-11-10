using System.Text.Json;
using ActusDesk.Domain.Pam;

namespace ActusDesk.IO;

/// <summary>
/// Simple contract data model for direct JSON loading
/// Supports simple contract arrays without test case structure
/// </summary>
public class SimpleContractData
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? InitialExchangeDate { get; set; }
    public string? MaturityDate { get; set; }
    public double? NotionalPrincipal { get; set; }
    public string? Currency { get; set; }
    public double? NominalInterestRate { get; set; }
    public string? DayCountConvention { get; set; }
    public string? CycleOfInterestPayment { get; set; }
    public string? CycleAnchorDateOfInterestPayment { get; set; }
    public string? ContractRole { get; set; }
    public string? StatusDate { get; set; }
}

/// <summary>
/// Mapper for simple contract JSON format (contract data only, no test expectations)
/// </summary>
public static class SimpleContractMapper
{
    /// <summary>
    /// Load simple contract array from JSON file
    /// Format: [ { "id": "PAM-001", "type": "PAM", ... }, ... ]
    /// </summary>
    public static async Task<List<SimpleContractData>> LoadSimpleContractsAsync(string filePath, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(filePath, ct);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        
        var contracts = JsonSerializer.Deserialize<List<SimpleContractData>>(json, options);
        return contracts ?? new List<SimpleContractData>();
    }

    /// <summary>
    /// Map simple contract data to PamContractModel
    /// </summary>
    public static PamContractModel MapToPamModel(SimpleContractData contract)
    {
        var model = new PamContractModel
        {
            ContractId = contract.Id ?? "",
            Currency = contract.Currency ?? "USD",
            ContractRole = contract.ContractRole ?? "RPA",
            NotionalPrincipal = contract.NotionalPrincipal ?? 0,
            NominalInterestRate = contract.NominalInterestRate,
            DayCountConvention = MapDayCountConvention(contract.DayCountConvention),
            ContractPerformance = "PF",
            NotionalScalingMultiplier = 1.0,
            InterestScalingMultiplier = 1.0
        };

        // Parse dates
        if (!string.IsNullOrWhiteSpace(contract.StatusDate))
            model.StatusDate = DateTime.Parse(contract.StatusDate);
        else
            model.StatusDate = DateTime.Now.Date;

        if (!string.IsNullOrWhiteSpace(contract.InitialExchangeDate))
            model.InitialExchangeDate = DateTime.Parse(contract.InitialExchangeDate);

        if (!string.IsNullOrWhiteSpace(contract.MaturityDate))
            model.MaturityDate = DateTime.Parse(contract.MaturityDate);

        // Parse cycles
        if (!string.IsNullOrWhiteSpace(contract.CycleOfInterestPayment))
            model.CycleOfInterestPayment = contract.CycleOfInterestPayment;

        if (!string.IsNullOrWhiteSpace(contract.CycleAnchorDateOfInterestPayment))
            model.CycleAnchorDateOfInterestPayment = DateTime.Parse(contract.CycleAnchorDateOfInterestPayment);

        return model;
    }

    /// <summary>
    /// Map day count convention to standard format
    /// </summary>
    private static string MapDayCountConvention(string? dcc)
    {
        if (string.IsNullOrWhiteSpace(dcc))
            return "30E/360";

        return dcc switch
        {
            "Act360" => "ACT/360",
            "Act365F" => "ACT/365",
            "Act365" => "ACT/365",
            "Thirty360" => "30/360",
            "30/360" => "30/360",
            "30E/360" => "30E/360",
            "ActAct" => "ACT/ACT",
            _ => dcc
        };
    }

    /// <summary>
    /// Detect if JSON file is in simple contract format or ACTUS test format
    /// </summary>
    public static async Task<bool> IsSimpleContractFormatAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            json = json.TrimStart();
            
            // Simple format starts with array: [
            // ACTUS test format starts with object: {
            return json.StartsWith("[");
        }
        catch
        {
            return false;
        }
    }
}
