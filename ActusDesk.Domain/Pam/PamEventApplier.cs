namespace ActusDesk.Domain.Pam;

/// <summary>
/// Interface for scenario-aware rate overrides.
/// Allows scenarios to provide rate shocks or alternative rates for rate reset events.
/// </summary>
public interface IPamScenario
{
    /// <summary>
    /// Try to get a rate override for a specific contract and event date
    /// </summary>
    /// <param name="contractId">Contract identifier</param>
    /// <param name="eventDate">Date of the rate reset event</param>
    /// <param name="rate">Output: the overridden rate if available</param>
    /// <returns>True if a rate override is provided, false otherwise</returns>
    bool TryGetRateOverride(string contractId, DateTime eventDate, out double rate);
}

/// <summary>
/// Event applier for PAM contracts.
/// Processes events in order and updates state, with scenario-aware rate reset handling.
/// </summary>
public static class PamEventApplier
{
    /// <summary>
    /// Apply events to a contract state
    /// </summary>
    /// <param name="events">List of events to apply</param>
    /// <param name="model">Contract model</param>
    /// <param name="scenario">Optional scenario for rate overrides</param>
    /// <param name="state">State to update</param>
    public static void ApplyEvents(
        List<PamEvent> events,
        PamContractModel model,
        IPamScenario? scenario,
        PamState state)
    {
        // Sort events to ensure proper order
        events.Sort(new PamEventComparer());

        // If purchase date is set, remove pre-purchase events (except AD)
        if (model.PurchaseDate.HasValue)
        {
            events.RemoveAll(e => e.EventDate < model.PurchaseDate.Value && e.EventType != PamEventType.AD);
        }

        foreach (var e in events)
        {
            switch (e.EventType)
            {
                case PamEventType.AD:
                    // Analysis date - no state transition
                    break;

                case PamEventType.IED:
                    ApplyIED(model, state, e.EventDate);
                    break;

                case PamEventType.MD:
                    ApplyMD(model, state, e.EventDate);
                    break;

                case PamEventType.PRD:
                    ApplyPRD(model, state, e.EventDate);
                    break;

                case PamEventType.IP:
                    ApplyIP(model, state, e.EventDate);
                    break;

                case PamEventType.IPCI:
                    ApplyIPCI(model, state, e.EventDate);
                    break;

                case PamEventType.RR:
                    ApplyRR(model, state, scenario, e.EventDate, false);
                    break;

                case PamEventType.RRF:
                    ApplyRR(model, state, scenario, e.EventDate, true);
                    break;

                case PamEventType.FP:
                    ApplyFP(model, state, e.EventDate);
                    break;

                case PamEventType.SC:
                    ApplySC(model, state, e.EventDate);
                    break;

                case PamEventType.TD:
                    ApplyTD(model, state, e.EventDate);
                    break;
            }
        }
    }

    private static void ApplyIED(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Initial exchange: set notional and rate
        state.NotionalPrincipal = RoleSign(model.ContractRole) * model.NotionalPrincipal;
        state.NominalInterestRate = model.NominalInterestRate ?? 0.0;
        state.StatusDate = eventDate;
    }

    private static void ApplyMD(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Maturity: principal is redeemed, notional goes to zero
        // Interest is paid out
        state.AccruedInterest = 0.0;
        state.NotionalPrincipal = 0.0;
        state.StatusDate = eventDate;
    }

    private static void ApplyPRD(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Purchase date: may adjust state based on purchase price
        // Simplified: just update status date
        state.StatusDate = eventDate;
    }

    private static void ApplyIP(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Interest payment: accrued interest is paid, reset to zero
        state.AccruedInterest = 0.0;
        state.StatusDate = eventDate;
        
        // In reality, accrued interest would be calculated here based on day count
        // For now, we just reset it
    }

    private static void ApplyIPCI(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Interest capitalization: add accrued interest to notional
        state.NotionalPrincipal += state.AccruedInterest;
        state.AccruedInterest = 0.0;
        state.StatusDate = eventDate;
    }

    private static void ApplyRR(
        PamContractModel model,
        PamState state,
        IPamScenario? scenario,
        DateTime eventDate,
        bool isFixed)
    {
        // Rate reset: update nominal interest rate
        
        // First, check if scenario provides an override
        if (scenario != null && scenario.TryGetRateOverride(model.ContractId, eventDate, out var newRate))
        {
            state.NominalInterestRate = newRate;
            state.StatusDate = eventDate;
            return;
        }

        // If this is RRF (fixed) and NextResetRate is available, use it
        if (isFixed && model.NextResetRate.HasValue)
        {
            state.NominalInterestRate = model.NextResetRate.Value;
            state.StatusDate = eventDate;
            return;
        }

        // Otherwise, keep current rate (no change)
        state.StatusDate = eventDate;
    }

    private static void ApplyFP(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Fee payment: pay accumulated fees
        state.FeeAccrued = 0.0;
        state.StatusDate = eventDate;
    }

    private static void ApplySC(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Scaling: apply scaling multipliers
        // This would typically involve updating notional or interest based on index
        // For now, just update status date
        state.StatusDate = eventDate;
    }

    private static void ApplyTD(PamContractModel model, PamState state, DateTime eventDate)
    {
        // Termination: contract ends early
        state.NotionalPrincipal = 0.0;
        state.AccruedInterest = 0.0;
        state.StatusDate = eventDate;
    }

    private static int RoleSign(string contractRole)
    {
        // RPA (Real Position Asset) = Lender/Receiver = -1
        // RPL (Real Position Liability) = Borrower/Payer = +1
        return contractRole?.Equals("RPL", StringComparison.OrdinalIgnoreCase) == true ? 1 : -1;
    }
}
