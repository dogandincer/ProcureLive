using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;
using ProcureLive.Domain.ValueObjects;

namespace ProcureLive.Domain.Entities.Auctions;

public class Bid : BaseEntity
{
    public Guid AuctionItemId { get; set; }

    public AuctionItem AuctionItem { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid UserId { get; set; }

    public Money Amount { get; set; } = null!;

    public DateTime BidTime { get; set; }

    public BidStatus Status { get; set; } = BidStatus.Active;

    public bool IsAutoBid { get; set; }

    public string? IpAddress { get; set; }
}
