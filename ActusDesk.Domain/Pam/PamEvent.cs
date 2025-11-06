namespace ActusDesk.Domain.Pam;

/// <summary>
/// PAM event types mirroring Java ACTUS reference implementation
/// </summary>
public enum PamEventType
{
    AD = 0,   // Analysis Date
    IED,      // Initial Exchange Date
    MD,       // Maturity Date
    PRD,      // Purchase Date
    IP,       // Interest Payment
    IPCI,     // Interest Capitalization
    RR,       // Rate Reset
    RRF,      // Rate Reset Fixed
    FP,       // Fee Payment
    SC,       // Scaling Index
    TD        // Termination Date
}

/// <summary>
/// GPU-friendly event structure with compact representation
/// </summary>
public struct PamEvent
{
    public DateTime EventDate;
    public PamEventType EventType;
    public string ContractId;
    public string Currency;
    public byte BdcCode;   // pre-resolved if needed
}

/// <summary>
/// Comparer for sorting events by date, then by event type
/// </summary>
public class PamEventComparer : IComparer<PamEvent>
{
    public int Compare(PamEvent x, PamEvent y)
    {
        int dateComparison = x.EventDate.CompareTo(y.EventDate);
        if (dateComparison != 0)
            return dateComparison;
        
        return x.EventType.CompareTo(y.EventType);
    }
}
