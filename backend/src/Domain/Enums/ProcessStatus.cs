namespace ProcureLive.Domain.Enums;

/// <summary>Shared lifecycle status for RFI/RFQ processes.</summary>
public enum ProcessStatus
{
    Draft = 0,
    Sent = 1,
    InProgress = 2,
    Closed = 3,
    Evaluated = 4,
    Awarded = 5
}
