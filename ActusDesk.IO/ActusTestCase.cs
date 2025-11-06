using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActusDesk.IO;

/// <summary>
/// ACTUS test case structure from reference implementation
/// </summary>
public sealed class ActusTestCase
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = "";

    [JsonPropertyName("terms")]
    public ActusTerms Terms { get; set; } = new();

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("dataObserved")]
    public JsonElement? DataObserved { get; set; }

    [JsonPropertyName("eventsObserved")]
    public JsonElement? EventsObserved { get; set; }

    [JsonPropertyName("results")]
    public List<ActusExpectedResult> Results { get; set; } = new();
}

/// <summary>
/// ACTUS contract terms from test JSON (using JsonElement for flexibility)
/// </summary>
public sealed class ActusTerms
{
    [JsonPropertyName("contractType")]
    public string? ContractType { get; set; }

    [JsonPropertyName("contractID")]
    public string? ContractID { get; set; }

    [JsonPropertyName("statusDate")]
    public string? StatusDate { get; set; }

    [JsonPropertyName("contractDealDate")]
    public string? ContractDealDate { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("notionalPrincipal")]
    public JsonElement? NotionalPrincipal { get; set; }

    [JsonPropertyName("initialExchangeDate")]
    public string? InitialExchangeDate { get; set; }

    [JsonPropertyName("maturityDate")]
    public string? MaturityDate { get; set; }

    [JsonPropertyName("nominalInterestRate")]
    public JsonElement? NominalInterestRate { get; set; }

    [JsonPropertyName("cycleAnchorDateOfInterestPayment")]
    public string? CycleAnchorDateOfInterestPayment { get; set; }

    [JsonPropertyName("cycleOfInterestPayment")]
    public string? CycleOfInterestPayment { get; set; }

    [JsonPropertyName("dayCountConvention")]
    public string? DayCountConvention { get; set; }

    [JsonPropertyName("endOfMonthConvention")]
    public string? EndOfMonthConvention { get; set; }

    [JsonPropertyName("businessDayConvention")]
    public string? BusinessDayConvention { get; set; }

    [JsonPropertyName("premiumDiscountAtIED")]
    public JsonElement? PremiumDiscountAtIED { get; set; }

    [JsonPropertyName("rateMultiplier")]
    public JsonElement? RateMultiplier { get; set; }

    [JsonPropertyName("contractRole")]
    public string? ContractRole { get; set; }

    [JsonPropertyName("purchaseDate")]
    public string? PurchaseDate { get; set; }

    [JsonPropertyName("terminationDate")]
    public string? TerminationDate { get; set; }

    [JsonPropertyName("capitalizationEndDate")]
    public string? CapitalizationEndDate { get; set; }

    [JsonPropertyName("cycleAnchorDateOfRateReset")]
    public string? CycleAnchorDateOfRateReset { get; set; }

    [JsonPropertyName("cycleOfRateReset")]
    public string? CycleOfRateReset { get; set; }

    [JsonPropertyName("nextResetRate")]
    public JsonElement? NextResetRate { get; set; }

    [JsonPropertyName("cycleAnchorDateOfFee")]
    public string? CycleAnchorDateOfFee { get; set; }

    [JsonPropertyName("cycleOfFee")]
    public string? CycleOfFee { get; set; }

    [JsonPropertyName("feeRate")]
    public JsonElement? FeeRate { get; set; }

    [JsonPropertyName("cycleAnchorDateOfScalingIndex")]
    public string? CycleAnchorDateOfScalingIndex { get; set; }

    [JsonPropertyName("cycleOfScalingIndex")]
    public string? CycleOfScalingIndex { get; set; }

    [JsonPropertyName("scalingEffect")]
    public string? ScalingEffect { get; set; }

    [JsonPropertyName("accruedInterest")]
    public JsonElement? AccruedInterest { get; set; }

    [JsonPropertyName("contractPerformance")]
    public string? ContractPerformance { get; set; }
}

/// <summary>
/// Expected result from ACTUS test case
/// </summary>
public sealed class ActusExpectedResult
{
    [JsonPropertyName("eventDate")]
    public string EventDate { get; set; } = "";

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "";

    [JsonPropertyName("payoff")]
    public double Payoff { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("notionalPrincipal")]
    public double NotionalPrincipal { get; set; }

    [JsonPropertyName("nominalInterestRate")]
    public double NominalInterestRate { get; set; }

    [JsonPropertyName("accruedInterest")]
    public double AccruedInterest { get; set; }
}
