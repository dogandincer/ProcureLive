namespace ProcureLive.Contracts.Common;

/// <summary>RFI/RFQ iptali; gerekçe davetli tedarikçilere bildirilir.</summary>
public sealed record CancelProcessRequest(string? Reason);
