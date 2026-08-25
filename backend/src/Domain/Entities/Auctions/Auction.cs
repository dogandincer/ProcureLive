using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Auctions;

public class Auction : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AuctionType AuctionType { get; set; }

    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

    public Guid OwnerCompanyId { get; set; }

    public Company OwnerCompany { get; set; } = null!;

    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public DateTime? ActualStartAt { get; set; }

    public DateTime? ActualEndAt { get; set; }

    public decimal? ReservePrice { get; set; }

    public decimal? MinDecrementAmount { get; set; }

    public decimal? MinDecrementPercentage { get; set; }

    /// <summary>Anti-sniping: if a bid lands within this many minutes of EndAt, EndAt is pushed out.</summary>
    public int ExtensionRuleMinutes { get; set; }

    public ICollection<AuctionItem> Items { get; set; } = new List<AuctionItem>();

    public ICollection<AuctionInvitation> Invitations { get; set; } = new List<AuctionInvitation>();

    public ICollection<AuctionExtensionLog> ExtensionLogs { get; set; } = new List<AuctionExtensionLog>();
}
