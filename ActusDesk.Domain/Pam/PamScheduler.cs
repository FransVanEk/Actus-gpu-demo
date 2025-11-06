namespace ActusDesk.Domain.Pam;

/// <summary>
/// PAM event scheduler mirroring Java ACTUS reference implementation.
/// Generates contract events based on contract terms and schedules.
/// </summary>
public static class PamScheduler
{
    /// <summary>
    /// Generate schedule of events for a PAM contract up to the specified date
    /// </summary>
    /// <param name="to">End date for event generation</param>
    /// <param name="model">PAM contract model</param>
    /// <returns>List of scheduled events</returns>
    public static List<PamEvent> Schedule(DateTime to, PamContractModel model)
    {
        var events = new List<PamEvent>();

        // 1. Add IED (Initial Exchange Date)
        if (model.InitialExchangeDate.HasValue)
        {
            events.Add(CreateEvent(model, model.InitialExchangeDate.Value, PamEventType.IED));
        }

        // 2. Add MD (Maturity Date)
        events.Add(CreateEvent(model, model.MaturityDate, PamEventType.MD));

        // 3. Add PRD (Purchase Date) if present
        if (model.PurchaseDate.HasValue)
        {
            events.Add(CreateEvent(model, model.PurchaseDate.Value, PamEventType.PRD));
        }

        // 4. Interest Payment Schedule
        AddInterestEvents(events, model);

        // 5. Rate Reset Schedule
        AddRateResetEvents(events, model);

        // 6. Fee Payment Schedule
        AddFeeEvents(events, model);

        // 7. Scaling Index Schedule
        AddScalingEvents(events, model);

        // 8. Termination Date handling
        if (model.TerminationDate.HasValue)
        {
            // Remove all events after termination
            events.RemoveAll(e => e.EventDate > model.TerminationDate.Value);
            // Add TD event
            events.Add(CreateEvent(model, model.TerminationDate.Value, PamEventType.TD));
        }

        // 9. Remove events before status date
        events.RemoveAll(e => e.EventDate < model.StatusDate);

        // 10. Remove events after 'to' date
        events.RemoveAll(e => e.EventDate > to);

        // 11. Sort events by date, then by type
        events.Sort(new PamEventComparer());

        return events;
    }

    private static void AddInterestEvents(List<PamEvent> events, PamContractModel model)
    {
        bool hasInterestRate = model.NominalInterestRate.HasValue;
        bool hasInterestCycle = !string.IsNullOrWhiteSpace(model.CycleOfInterestPayment) || 
                                model.CycleAnchorDateOfInterestPayment.HasValue;

        if (hasInterestRate && hasInterestCycle)
        {
            // Build interest schedule
            var anchor = model.CycleAnchorDateOfInterestPayment ?? 
                        model.InitialExchangeDate ?? 
                        model.StatusDate;
            
            var cycle = model.CycleOfInterestPayment ?? "1Y"; // fallback

            var ipDates = ScheduleFactory.GenerateSchedule(
                anchor, 
                model.MaturityDate, 
                cycle, 
                model.EndOfMonthConvention,
                true).ToList();

            // Add IP events
            foreach (var date in ipDates)
            {
                if (date > (model.InitialExchangeDate ?? model.StatusDate))
                {
                    events.Add(CreateEvent(model, date, PamEventType.IP));
                }
            }

            // Handle capitalization
            if (model.CapitalizationEndDate.HasValue)
            {
                var capDate = model.CapitalizationEndDate.Value;

                // Remove IP at capitalization date if exists
                events.RemoveAll(e => e.EventType == PamEventType.IP && e.EventDate == capDate);

                // Add IPCI at capitalization date
                events.Add(CreateEvent(model, capDate, PamEventType.IPCI));

                // Convert all IPs before or at capitalization date to IPCI
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].EventType == PamEventType.IP && events[i].EventDate <= capDate)
                    {
                        var ipci = events[i];
                        ipci.EventType = PamEventType.IPCI;
                        events[i] = ipci;
                    }
                }
            }
        }
        else if (model.CapitalizationEndDate.HasValue)
        {
            // Only capitalization date, no interest schedule
            events.Add(CreateEvent(model, model.CapitalizationEndDate.Value, PamEventType.IPCI));
        }
        else if (hasInterestRate && !hasInterestCycle)
        {
            // Fallback: create simple IP schedule from IED to maturity
            var start = model.InitialExchangeDate ?? model.StatusDate;
            var anchor = start;
            var cycle = "1Y"; // Default annual schedule

            var ipDates = ScheduleFactory.GenerateSchedule(
                anchor,
                model.MaturityDate,
                cycle,
                model.EndOfMonthConvention,
                true).ToList();

            foreach (var date in ipDates)
            {
                if (date > start && date <= model.MaturityDate)
                {
                    events.Add(CreateEvent(model, date, PamEventType.IP));
                }
            }
        }
    }

    private static void AddRateResetEvents(List<PamEvent> events, PamContractModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CycleOfRateReset))
            return;

        var anchor = model.CycleAnchorDateOfRateReset ?? 
                    model.InitialExchangeDate ?? 
                    model.StatusDate;

        var rrDates = ScheduleFactory.GenerateSchedule(
            anchor,
            model.MaturityDate,
            model.CycleOfRateReset,
            model.EndOfMonthConvention,
            true).ToList();

        foreach (var date in rrDates)
        {
            if (date >= model.StatusDate)
            {
                events.Add(CreateEvent(model, date, PamEventType.RR));
            }
        }

        // Handle NextResetRate (convert first RR after status date to RRF)
        if (model.NextResetRate.HasValue)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].EventType == PamEventType.RR && events[i].EventDate > model.StatusDate)
                {
                    var rrf = events[i];
                    rrf.EventType = PamEventType.RRF;
                    events[i] = rrf;
                    break; // Only the first one
                }
            }
        }
    }

    private static void AddFeeEvents(List<PamEvent> events, PamContractModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CycleOfFee))
            return;

        var anchor = model.CycleAnchorDateOfFee ?? 
                    model.InitialExchangeDate ?? 
                    model.StatusDate;

        var fpDates = ScheduleFactory.GenerateSchedule(
            anchor,
            model.MaturityDate,
            model.CycleOfFee,
            model.EndOfMonthConvention,
            true).ToList();

        foreach (var date in fpDates)
        {
            if (date >= model.StatusDate)
            {
                events.Add(CreateEvent(model, date, PamEventType.FP));
            }
        }
    }

    private static void AddScalingEvents(List<PamEvent> events, PamContractModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ScalingEffect))
            return;

        // Check if scaling effect contains "I" or "N"
        bool hasScaling = model.ScalingEffect.Contains("I", StringComparison.OrdinalIgnoreCase) ||
                         model.ScalingEffect.Contains("N", StringComparison.OrdinalIgnoreCase);

        if (!hasScaling || string.IsNullOrWhiteSpace(model.CycleOfScalingIndex))
            return;

        var anchor = model.CycleAnchorDateOfScalingIndex ?? 
                    model.InitialExchangeDate ?? 
                    model.StatusDate;

        var scDates = ScheduleFactory.GenerateSchedule(
            anchor,
            model.MaturityDate,
            model.CycleOfScalingIndex,
            model.EndOfMonthConvention,
            true).ToList();

        foreach (var date in scDates)
        {
            if (date >= model.StatusDate)
            {
                events.Add(CreateEvent(model, date, PamEventType.SC));
            }
        }
    }

    private static PamEvent CreateEvent(PamContractModel model, DateTime date, PamEventType eventType)
    {
        return new PamEvent
        {
            EventDate = date,
            EventType = eventType,
            ContractId = model.ContractId,
            Currency = model.Currency,
            BdcCode = 0 // Can be enhanced later with business day convention resolution
        };
    }
}
