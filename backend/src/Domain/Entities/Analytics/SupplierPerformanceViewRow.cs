namespace ProcureLive.Domain.Entities.Analytics;

/// <summary>Read-only projection of the "SupplierPerformanceView" database view (see AddAnalyticsViews migration).</summary>
public class SupplierPerformanceViewRow
{
    public Guid CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public decimal? AverageRfiScore { get; set; }

    public int RfiEvaluationCount { get; set; }

    public int SubmittedRfqQuoteCount { get; set; }

    public int TotalAuctionsParticipated { get; set; }

    public int WonAuctionCount { get; set; }

    public decimal? AuctionWinRatePercentage { get; set; }

    public double? AverageResponseHours { get; set; }
}
