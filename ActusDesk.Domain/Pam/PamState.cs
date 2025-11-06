namespace ActusDesk.Domain.Pam;

/// <summary>
/// PAM contract state mirroring Java ACTUS reference implementation.
/// Tracks the evolving state as events are applied.
/// </summary>
public sealed class PamState
{
    public double NotionalPrincipal;
    public double NominalInterestRate;
    public double AccruedInterest;
    public double FeeAccrued;
    public double NotionalScalingMultiplier;
    public double InterestScalingMultiplier;
    public DateTime StatusDate;
    public string ContractPerformance = "PF";

    /// <summary>
    /// Initialize state from contract model
    /// </summary>
    public static PamState InitFrom(PamContractModel model, Func<DateTime, DateTime, double>? dayCountFn = null)
    {
        var state = new PamState
        {
            NotionalScalingMultiplier = model.NotionalScalingMultiplier,
            InterestScalingMultiplier = model.InterestScalingMultiplier,
            ContractPerformance = model.ContractPerformance,
            StatusDate = model.StatusDate
        };

        // If IED is in the future, state starts with zero notional
        if (model.InitialExchangeDate.HasValue && model.InitialExchangeDate.Value > model.StatusDate)
        {
            state.NotionalPrincipal = 0.0;
            state.NominalInterestRate = 0.0;
        }
        else
        {
            state.NotionalPrincipal = RoleSign(model.ContractRole) * model.NotionalPrincipal;
            state.NominalInterestRate = model.NominalInterestRate ?? 0.0;
        }

        // Set accrued interest
        if (model.AccruedInterest.HasValue)
        {
            state.AccruedInterest = model.AccruedInterest.Value;
        }
        else
        {
            state.AccruedInterest = 0.0;
            // TODO: Could calculate using dayCountFn if needed
        }

        // Set fee accrued
        if (model.FeeRate.HasValue)
        {
            state.FeeAccrued = model.FeeRate.Value;
        }
        else
        {
            state.FeeAccrued = 0.0;
        }

        return state;
    }

    /// <summary>
    /// Determine role sign for contract (payer vs receiver)
    /// </summary>
    private static int RoleSign(string contractRole)
    {
        // RPA (Real Position Asset) = Lender/Receiver = -1
        // RPL (Real Position Liability) = Borrower/Payer = +1
        // Check for RPL specifically, otherwise assume RPA pattern
        return contractRole?.Equals("RPL", StringComparison.OrdinalIgnoreCase) == true ? 1 : -1;
    }
}
