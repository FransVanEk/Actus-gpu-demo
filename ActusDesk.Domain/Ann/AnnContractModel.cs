namespace ActusDesk.Domain.Ann;

/// <summary>
/// Simplified ANN contract model mirroring Java ACTUS reference implementation.
/// GPU-friendly flat structure with all fields needed for scheduling and application.
/// </summary>
public sealed class AnnContractModel
{
    public string ContractId { get; set; } = "";
    public string Currency { get; set; } = "EUR";

    public DateTime StatusDate { get; set; }
    public DateTime? InitialExchangeDate { get; set; }
    public DateTime MaturityDate { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public DateTime? CapitalizationEndDate { get; set; }

    public DateTime? CycleAnchorDateOfInterestPayment { get; set; }
    public string? CycleOfInterestPayment { get; set; }

    public DateTime? CycleAnchorDateOfPrincipalRedemption { get; set; }
    public string? CycleOfPrincipalRedemption { get; set; }

    public DateTime? CycleAnchorDateOfRateReset { get; set; }
    public string? CycleOfRateReset { get; set; }

    public DateTime? CycleAnchorDateOfFee { get; set; }
    public string? CycleOfFee { get; set; }

    public DateTime? CycleAnchorDateOfScalingIndex { get; set; }
    public string? CycleOfScalingIndex { get; set; }

    public string? EndOfMonthConvention { get; set; }
    public string? BusinessDayConvention { get; set; }

    public double? NominalInterestRate { get; set; }
    public double NotionalPrincipal { get; set; }
    public string ContractRole { get; set; } = "RPA"; // for roleSign

    public double? AccruedInterest { get; set; }
    public double? FeeRate { get; set; }
    public double NotionalScalingMultiplier { get; set; } = 1.0;
    public double InterestScalingMultiplier { get; set; } = 1.0;

    public string ContractPerformance { get; set; } = "PF";
    public double? NextResetRate { get; set; }
    public double? NextPrincipalRedemptionPayment { get; set; }

    public string? ScalingEffect { get; set; } // to check contains("I") or ("N")
    public string DayCountConvention { get; set; } = "30E/360";
    public string? InterestCalculationBase { get; set; } // NT or other
}
