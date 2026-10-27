using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Auctions.Commands.AddAuctionItem;
using ProcureLive.Application.Auctions.Commands.AwardAuctionItem;
using ProcureLive.Application.Auctions.Commands.CancelAuction;
using ProcureLive.Application.Auctions.Commands.ChangeAuctionStatus;
using ProcureLive.Application.Auctions.Commands.CreateAuction;
using ProcureLive.Application.Auctions.Commands.CreateTender;
using ProcureLive.Application.Auctions.Commands.ExtendAuction;
using ProcureLive.Application.Auctions.Commands.InviteCompanies;
using ProcureLive.Application.Auctions.Commands.JoinAuction;
using ProcureLive.Application.Auctions.Commands.PlaceBid;
using ProcureLive.Application.Auctions.Commands.PublishAuction;
using ProcureLive.Application.Auctions.Commands.RespondToInvitation;
using ProcureLive.Application.Auctions.Participation;
using ProcureLive.Application.Auctions.Queries.GetAuctionById;
using ProcureLive.Application.Auctions.Queries.GetAuctionsList;
using ProcureLive.Application.Auctions.Queries.GetAuctionStatus;
using ProcureLive.Application.Auctions.Queries.GetItemBidsForComparison;
using ProcureLive.Application.Auctions.Queries.GetMyAuctionHistory;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Invitations.Commands;
using ProcureLive.Application.Invitations.Queries;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Invitations;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/auctions")]
[Authorize]
public sealed class AuctionsController : ControllerBase
{
    private readonly ISender _sender;

    public AuctionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuctionDto>>> GetList(
        [FromQuery] string? status,
        [FromQuery] Guid? firmId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<AuctionDto> result = await _sender.Send(new GetAuctionsListQuery(status, page, pageSize, firmId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("history/mine")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<PagedResult<AuctionDto>>> GetMyHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<AuctionDto> result = await _sender.Send(new GetMyAuctionHistoryQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuctionDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        AuctionDetailDto result = await _sender.Send(new GetAuctionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateAuctionRequest request, CancellationToken cancellationToken)
    {
        AuctionType auctionType;
        bool parsed = Enum.TryParse(request.AuctionType, true, out auctionType);

        if (!parsed)
        {
            return BadRequest($"Geçersiz AuctionType: {request.AuctionType}");
        }

        Guid id = await _sender.Send(
            new CreateAuctionCommand(
                request.Title, request.Description, auctionType, request.OwnerCompanyId, request.CurrencyId,
                request.StartAt, request.EndAt, request.ReservePrice, request.MinDecrementAmount,
                request.MinDecrementPercentage, request.ExtensionRuleMinutes, request.IsListedForPlus, request.WorkspaceId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Tek payload ile ihale + kalemler + davetler + ekleri (master-detail) oluşturur.</summary>
    [HttpPost("tender")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> CreateTender(CreateTenderRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.Master.AuctionType, true, out AuctionType auctionType))
            return BadRequest($"Geçersiz AuctionType: {request.Master.AuctionType}");

        PaymentTerm? paymentTerm = null;
        if (!string.IsNullOrWhiteSpace(request.Master.PaymentTerm))
        {
            if (!Enum.TryParse(request.Master.PaymentTerm, true, out PaymentTerm pt))
                return BadRequest($"Geçersiz PaymentTerm: {request.Master.PaymentTerm}");
            paymentTerm = pt;
        }

        DeliveryTerm? deliveryTerm = null;
        if (!string.IsNullOrWhiteSpace(request.Master.DeliveryTerm))
        {
            if (!Enum.TryParse(request.Master.DeliveryTerm, true, out DeliveryTerm dt))
                return BadRequest($"Geçersiz DeliveryTerm: {request.Master.DeliveryTerm}");
            deliveryTerm = dt;
        }

        var lines = new List<TenderLineInput>(request.Lines.Count);
        foreach (TenderLineRequest l in request.Lines)
        {
            if (!Enum.TryParse(l.UnitOfMeasure, true, out UnitOfMeasure uom))
                return BadRequest($"Geçersiz UnitOfMeasure: {l.UnitOfMeasure} (kalem: {l.Description})");

            lines.Add(new TenderLineInput(
                l.ProductId, l.Description, l.Quantity, uom, l.TargetPrice,
                l.RequestedDeliveryDate, l.Specification, l.SortOrder));
        }

        var attachments = request.Attachments
            .Select(a => new TenderAttachmentInput(a.FileName, a.Url, a.ContentType, a.SizeBytes))
            .ToList();

        Guid id = await _sender.Send(
            new CreateTenderCommand(
                request.Master.ReferenceCode, request.Master.Title, request.Master.Description, auctionType,
                request.Master.OwnerCompanyId, request.Master.CurrencyId, request.Master.StartAt, request.Master.EndAt,
                request.Master.ReservePrice, request.Master.ExtensionTriggerMinutes, request.Master.ExtensionDurationMinutes,
                request.Master.MaxExtensionMinutes, paymentTerm, deliveryTerm,
                request.Master.AllowAlternativeProducts, request.Master.AllowPartialBids,
                lines, request.VendorIds, attachments, request.Master.IsListedForPlus, request.Master.WorkspaceId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> AddItem(Guid id, AddAuctionItemRequest request, CancellationToken cancellationToken)
    {
        UnitOfMeasure unitOfMeasure;
        bool parsed = Enum.TryParse(request.UnitOfMeasure, true, out unitOfMeasure);

        if (!parsed)
        {
            return BadRequest($"Geçersiz UnitOfMeasure: {request.UnitOfMeasure}");
        }

        Guid itemId = await _sender.Send(
            new AddAuctionItemCommand(id, request.ProductId, request.Description, request.Quantity, unitOfMeasure, request.TargetPrice, request.SortOrder),
            cancellationToken);

        return Ok(itemId);
    }

    [HttpPost("{id:guid}/invitations")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> InviteCompanies(Guid id, InviteCompaniesRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new InviteCompaniesCommand(id, request.CompanyIds), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/invitations/respond")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<IActionResult> RespondToInvitation(Guid id, RespondToInvitationRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new RespondToInvitationCommand(id, request.Accept, request.DeclineReason), cancellationToken);
        return NoContent();
    }

    /// <summary>Görünür açık ihaleye doğrudan katılım (kota: Standart üyede en fazla 2 ihale).</summary>
    [HttpPost("{id:guid}/join")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new JoinAuctionCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Davetsiz tedarikçinin katılım talebi + kalem bazında ön teklifi.</summary>
    [HttpPost("{id:guid}/participation-requests")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<Guid>> CreateParticipationRequest(
        Guid id, CreateParticipationRequestRequest request, CancellationToken cancellationToken)
    {
        Guid requestId = await _sender.Send(new CreateParticipationRequestCommand(id, request.Note, request.Lines), cancellationToken);
        return Ok(requestId);
    }

    [HttpGet("{id:guid}/participation-requests")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<IReadOnlyList<ParticipationRequestDto>>> GetParticipationRequests(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetParticipationRequestsQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/participation-requests/{requestId:guid}/approve-company")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> ApproveParticipationCompany(Guid id, Guid requestId, CancellationToken cancellationToken)
    {
        await _sender.Send(new ApproveParticipationCompanyCommand(id, requestId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/participation-requests/{requestId:guid}/accept")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> AcceptParticipationRequest(Guid id, Guid requestId, CancellationToken cancellationToken)
    {
        await _sender.Send(new AcceptParticipationRequestCommand(id, requestId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/participation-requests/{requestId:guid}/reject")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> RejectParticipationRequest(
        Guid id, Guid requestId, RejectParticipationRequestRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new RejectParticipationRequestCommand(id, requestId, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/invite-links")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<IReadOnlyList<InviteLinkDto>>> GetInviteLinks(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<InviteLinkDto> result = await _sender.Send(new GetInviteLinksQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Bir cari için benzersiz, süreli davet linki üretir (/invite?token=...).</summary>
    [HttpPost("{id:guid}/invite-links")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<InviteLinkDto>> CreateInviteLink(
        Guid id, CreateInviteLinkRequest request, CancellationToken cancellationToken)
    {
        InviteLinkDto result = await _sender.Send(
            new CreateInviteLinkCommand(id, request.CompanyId, request.Email, request.ValidDays), cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new PublishAuctionCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason, CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelAuctionCommand(id, reason), cancellationToken);
        return NoContent();
    }

    /// <summary>Durum paneli: mevcut durum, yapılabilecek geçişler ve durum geçmişi.</summary>
    [HttpGet("{id:guid}/status")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<AuctionStatusPanelDto>> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        AuctionStatusPanelDto result = await _sender.Send(new GetAuctionStatusQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Elle durum değişikliği (Planlandı→Canlı, Canlı→Duraklatıldı/Sona Erdi/İptal ...). İzinli geçişler ve
    /// kurallar AuctionStatusMachine'dedir; yalnızca Admin.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeAuctionStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.Status, true, out AuctionStatus target) || !Enum.IsDefined(target))
        {
            return BadRequest($"Geçersiz durum: {request.Status}");
        }

        await _sender.Send(
            new ChangeAuctionStatusCommand(id, target, request.Reason, request.NewStartAt, request.NewEndAt), cancellationToken);
        return NoContent();
    }

    /// <summary>Admin manual intervention: "23:00'da bitecek ihaleyi anında 23:01 yapabilme."</summary>
    [HttpPost("{id:guid}/extend")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Extend(Guid id, ExtendAuctionRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ExtendAuctionCommand(id, request.NewEndAt, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/bids")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<BidDto>> PlaceBid(Guid id, Guid itemId, PlaceBidRequest request, CancellationToken cancellationToken)
    {
        BidDto result = await _sender.Send(new PlaceBidCommand(id, itemId, request.Amount, request.CurrencyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/items/{itemId:guid}/bids/compare")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<IReadOnlyCollection<BidComparisonDto>>> CompareBids(
        Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BidComparisonDto> result = await _sender.Send(
            new GetItemBidsForComparisonQuery(id, itemId), cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/award")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> AwardItem(Guid id, Guid itemId, AwardAuctionItemRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AwardAuctionItemCommand(id, itemId, request.WinningBidId, request.Notes), cancellationToken);
        return NoContent();
    }
}
