using System.Globalization;
using System.Text.Json;
using ActusDesk.Domain.Pam;

namespace ActusDesk.IO;

/// <summary>
/// Mapper for converting ACTUS test JSON to PamContractModel
/// </summary>
public static class ActusPamMapper
{
    /// <summary>
    /// Load ACTUS PAM test cases from JSON file
    /// </summary>
    public static async Task<Dictionary<string, ActusTestCase>> LoadTestCasesAsync(string filePath, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(filePath, ct);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        
        var testCases = JsonSerializer.Deserialize<Dictionary<string, ActusTestCase>>(json, options);
        return testCases ?? new Dictionary<string, ActusTestCase>();
    }

    /// <summary>
    /// Map ACTUS terms to PamContractModel
    /// </summary>
    public static PamContractModel MapToPamModel(ActusTerms terms)
    {
        var model = new PamContractModel
        {
            ContractId = terms.ContractID ?? "",
            Currency = terms.Currency ?? "USD",
            ContractRole = terms.ContractRole ?? "RPA"
        };

        // Parse dates
        if (!string.IsNullOrWhiteSpace(terms.StatusDate))
            model.StatusDate = ParseDate(terms.StatusDate);

        if (!string.IsNullOrWhiteSpace(terms.InitialExchangeDate))
            model.InitialExchangeDate = ParseDate(terms.InitialExchangeDate);

        if (!string.IsNullOrWhiteSpace(terms.MaturityDate))
            model.MaturityDate = ParseDate(terms.MaturityDate);

        if (!string.IsNullOrWhiteSpace(terms.PurchaseDate))
            model.PurchaseDate = ParseDate(terms.PurchaseDate);

        if (!string.IsNullOrWhiteSpace(terms.TerminationDate))
            model.TerminationDate = ParseDate(terms.TerminationDate);

        if (!string.IsNullOrWhiteSpace(terms.CapitalizationEndDate))
            model.CapitalizationEndDate = ParseDate(terms.CapitalizationEndDate);

        // Parse interest payment cycle
        if (!string.IsNullOrWhiteSpace(terms.CycleAnchorDateOfInterestPayment))
            model.CycleAnchorDateOfInterestPayment = ParseDate(terms.CycleAnchorDateOfInterestPayment);

        if (!string.IsNullOrWhiteSpace(terms.CycleOfInterestPayment))
            model.CycleOfInterestPayment = NormalizeCycle(terms.CycleOfInterestPayment);

        // Parse rate reset cycle
        if (!string.IsNullOrWhiteSpace(terms.CycleAnchorDateOfRateReset))
            model.CycleAnchorDateOfRateReset = ParseDate(terms.CycleAnchorDateOfRateReset);

        if (!string.IsNullOrWhiteSpace(terms.CycleOfRateReset))
            model.CycleOfRateReset = NormalizeCycle(terms.CycleOfRateReset);

        // Parse fee cycle
        if (!string.IsNullOrWhiteSpace(terms.CycleAnchorDateOfFee))
            model.CycleAnchorDateOfFee = ParseDate(terms.CycleAnchorDateOfFee);

        if (!string.IsNullOrWhiteSpace(terms.CycleOfFee))
            model.CycleOfFee = NormalizeCycle(terms.CycleOfFee);

        // Parse scaling cycle
        if (!string.IsNullOrWhiteSpace(terms.CycleAnchorDateOfScalingIndex))
            model.CycleAnchorDateOfScalingIndex = ParseDate(terms.CycleAnchorDateOfScalingIndex);

        if (!string.IsNullOrWhiteSpace(terms.CycleOfScalingIndex))
            model.CycleOfScalingIndex = NormalizeCycle(terms.CycleOfScalingIndex);

        // Parse numeric values
        if (terms.NotionalPrincipal.HasValue)
            model.NotionalPrincipal = ParseJsonElement(terms.NotionalPrincipal.Value);

        if (terms.NominalInterestRate.HasValue)
            model.NominalInterestRate = ParseJsonElement(terms.NominalInterestRate.Value);

        if (terms.NextResetRate.HasValue)
            model.NextResetRate = ParseJsonElement(terms.NextResetRate.Value);

        if (terms.FeeRate.HasValue)
            model.FeeRate = ParseJsonElement(terms.FeeRate.Value);

        if (terms.AccruedInterest.HasValue)
            model.AccruedInterest = ParseJsonElement(terms.AccruedInterest.Value);

        // Parse conventions
        model.DayCountConvention = MapDayCountConvention(terms.DayCountConvention);
        model.EndOfMonthConvention = terms.EndOfMonthConvention;
        model.BusinessDayConvention = terms.BusinessDayConvention;
        model.ScalingEffect = terms.ScalingEffect;
        model.ContractPerformance = terms.ContractPerformance ?? "PF";

        // Parse multipliers
        if (terms.RateMultiplier.HasValue)
        {
            var multiplier = ParseJsonElement(terms.RateMultiplier.Value);
            model.InterestScalingMultiplier = multiplier;
            model.NotionalScalingMultiplier = multiplier;
        }

        return model;
    }

    /// <summary>
    /// Parse JsonElement to double, handling both string and number types
    /// </summary>
    private static double ParseJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.GetDouble();
            
            case JsonValueKind.String:
                var str = element.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    return 0;
                return ParseDouble(str);
            
            default:
                return 0;
        }
    }

    /// <summary>
    /// Parse date from ACTUS format (ISO 8601)
    /// </summary>
    private static DateTime ParseDate(string dateString)
    {
        // ACTUS uses ISO 8601: "2013-01-01T00:00:00"
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        throw new FormatException($"Invalid date format: {dateString}");
    }

    /// <summary>
    /// Parse double from string, handling whitespace
    /// </summary>
    private static double ParseDouble(string value)
    {
        var trimmed = value.Trim();
        if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        throw new FormatException($"Invalid numeric format: {value}");
    }

    /// <summary>
    /// Normalize ACTUS cycle format to simplified format
    /// ACTUS: "P1ML0" -> "1M"
    /// ACTUS: "P3ML1" -> "3M"
    /// </summary>
    private static string NormalizeCycle(string actusCycle)
    {
        // ACTUS cycle format: P{n}M[L{0|1}] where L0=short stub, L1=long stub
        // We'll simplify to just the period part
        if (string.IsNullOrWhiteSpace(actusCycle))
            return "";

        // Remove 'P' prefix if present
        var cycle = actusCycle.TrimStart('P', 'p');

        // Remove stub suffix (L0, L1)
        if (cycle.Contains('L'))
        {
            var lIndex = cycle.IndexOf('L');
            cycle = cycle.Substring(0, lIndex);
        }

        // Ensure it has the right format (e.g., "1M", "3M", "1Y")
        if (!cycle.StartsWith("P", StringComparison.OrdinalIgnoreCase))
            cycle = "P" + cycle;

        return cycle;
    }

    /// <summary>
    /// Map ACTUS day count convention to our format
    /// </summary>
    private static string MapDayCountConvention(string? actusDcc)
    {
        if (string.IsNullOrWhiteSpace(actusDcc))
            return "30E/360";

        return actusDcc switch
        {
            "A365" => "ACT/365",
            "A360" => "ACT/360",
            "30E360" => "30E/360",
            "30/360" => "30/360",
            "AA" => "ACT/ACT",
            _ => actusDcc
        };
    }
}
