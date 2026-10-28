using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Orders;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Orders;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Orders;

/// <summary>
/// Sipariş akışı: sonuçlanan ihale (kalem bazlı kazananlar → tedarikçi başına bir sipariş) veya RFQ'dan (kazanan teklif)
/// taslak açılır; satınalmacı gönderir, tedarikçi onaylar. Taslak tedarikçiye görünmez. Aynı kaynaktan aynı tedarikçiye
/// iptal edilmemiş sipariş varken yenisi açılmaz (tekrar çağrı güvenli).
/// </summary>
internal static class PurchaseOrderRules
{
    public const string RelatedType = nameof(PurchaseOrder);

    public static async Task<string> NextNumberAsync(IApplicationDbContext context, DateTime now, int offset, CancellationToken ct)
    {
        string prefix = $"PO-{now:yyyy}-";
        List<string> numbers = await context.PurchaseOrders.Where(o => o.OrderNumber.StartsWith(prefix))
            .Select(o => o.OrderNumber).ToListAsync(ct);
        int max = numbers.Select(n => int.TryParse(n[prefix.Length..], out int v) ? v : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{max + 1 + offset:0000}";
    }

    /// <summary>Tedarikçi yalnızca kendisine gönderilmiş siparişi; satınalmacı yalnızca kapsamındaki firmanın siparişini görür.</summary>
    public static async Task EnsureVisibleAsync(
        PurchaseOrder order, ICurrentUserService user, IDataScopeService scopes, CancellationToken ct)
    {
        if (user.IsInRole(Roles.Admin)) return;
        if (user.IsInRole(Roles.Buyer))
        {
            DataScope scope = await scopes.GetAsync(ct);
            if (!scope.HasOwned(order.WorkspaceId, order.OwnerCompanyId))
                throw new ForbiddenAccessException("Bu kayıt yetkili olduğunuz firma/işyerlerine ait değil.");
            return;
        }

        if (order.SupplierCompanyId != user.CompanyId || order.Status == PurchaseOrderStatus.Draft)
            throw new NotFoundException(nameof(PurchaseOrder), order.Id);
    }

    public static PurchaseOrderLine Line(Guid? sourceItemId, string description, decimal quantity, UnitOfMeasure unit, decimal unitPrice, int sort) =>
        new()
        {
            SourceItemId = sourceItemId,
            Description = description,
            Quantity = quantity,
            UnitOfMeasure = unit,
            UnitPrice = unitPrice,
            LineTotal = Math.Round(quantity * unitPrice, 2),
            SortOrder = sort,
        };
}

// ---------------------------------------------------------------- Oluşturma

[RequiresPermission(PermissionCodes.OrdersManage)]
public sealed record CreateOrdersFromAuctionCommand(Guid AuctionId) : IRequest<IReadOnlyList<Guid>>, IAuctionScopedRequest;

[RequiresPermission(PermissionCodes.OrdersManage)]
public sealed record CreateOrderFromRfqCommand(Guid RfqProcessId) : IRequest<IReadOnlyList<Guid>>, IRfqScopedRequest;

public sealed class CreateOrdersFromAuctionCommandHandler : IRequestHandler<CreateOrdersFromAuctionCommand, IReadOnlyList<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public CreateOrdersFromAuctionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<Guid>> Handle(CreateOrdersFromAuctionCommand r, CancellationToken cancellationToken)
    {
        Auction auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == r.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), r.AuctionId);
        List<AuctionAward> awards = await _context.AuctionAwards
            .Include(a => a.AuctionItem).Include(a => a.WinningBid)
            .Where(a => a.AuctionId == auction.Id)
            .ToListAsync(cancellationToken);
        if (awards.Count == 0)
            throw new ConflictException("Sipariş için önce en az bir kalemde kazanan seçilmelidir.");

        HashSet<Guid> alreadyOrdered = (await _context.PurchaseOrders
            .Where(o => o.SourceKind == ProcessKind.Auction && o.SourceId == auction.Id && o.Status != PurchaseOrderStatus.Cancelled)
            .Select(o => o.SupplierCompanyId).ToListAsync(cancellationToken)).ToHashSet();

        DateTime now = _dateTime.UtcNow;
        var created = new List<PurchaseOrder>();
        foreach (var group in awards.Where(a => !alreadyOrdered.Contains(a.AwardedCompanyId))
                     .GroupBy(a => a.AwardedCompanyId))
        {
            List<AuctionAward> lines = group.OrderBy(a => a.AuctionItem.SortOrder).ToList();
            var order = new PurchaseOrder
            {
                OrderNumber = await PurchaseOrderRules.NextNumberAsync(_context, now, created.Count, cancellationToken),
                SourceKind = ProcessKind.Auction,
                SourceId = auction.Id,
                SourceTitle = auction.Title,
                OwnerCompanyId = auction.OwnerCompanyId,
                WorkspaceId = auction.WorkspaceId,
                SupplierCompanyId = group.Key,
                CurrencyId = lines[0].WinningBid.Amount.CurrencyId,
                DeliveryDate = lines.Select(a => a.AuctionItem.RequestedDeliveryDate).Where(d => d.HasValue).Min(),
                CreatedByUserId = _currentUser.UserId,
            };
            int sort = 0;
            foreach (AuctionAward award in lines)
            {
                order.Lines.Add(PurchaseOrderRules.Line(award.AuctionItemId, award.AuctionItem.Description, award.AuctionItem.Quantity,
                    award.AuctionItem.UnitOfMeasure, award.WinningBid.Amount.Amount, sort++));
            }
            order.TotalAmount = order.Lines.Sum(l => l.LineTotal);
            _context.PurchaseOrders.Add(order);
            created.Add(order);
        }

        if (created.Count == 0)
            throw new ConflictException("Bu ihalenin tüm kazananları için sipariş zaten açılmış.");
        await _context.SaveChangesAsync(cancellationToken);
        return created.Select(o => o.Id).ToList();
    }
}

public sealed class CreateOrderFromRfqCommandHandler : IRequestHandler<CreateOrderFromRfqCommand, IReadOnlyList<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public CreateOrderFromRfqCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<Guid>> Handle(CreateOrderFromRfqCommand r, CancellationToken cancellationToken)
    {
        RfqProcess process = await _context.RfqProcesses.FirstOrDefaultAsync(p => p.Id == r.RfqProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfqProcess), r.RfqProcessId);
        if (process.WinningQuoteId is not Guid quoteId)
            throw new ConflictException("Sipariş için önce kazanan teklif seçilmelidir.");
        RfqQuote quote = await _context.RfqQuotes.Include(q => q.LineItems).ThenInclude(l => l.RfqLineItem)
            .FirstAsync(q => q.Id == quoteId, cancellationToken);

        if (await _context.PurchaseOrders.AnyAsync(o => o.SourceKind == ProcessKind.Rfq && o.SourceId == process.Id
                && o.SupplierCompanyId == quote.CompanyId && o.Status != PurchaseOrderStatus.Cancelled, cancellationToken))
            throw new ConflictException("Bu RFQ için sipariş zaten açılmış.");

        var order = new PurchaseOrder
        {
            OrderNumber = await PurchaseOrderRules.NextNumberAsync(_context, _dateTime.UtcNow, 0, cancellationToken),
            SourceKind = ProcessKind.Rfq,
            SourceId = process.Id,
            SourceTitle = process.Title,
            OwnerCompanyId = process.OwnerCompanyId,
            WorkspaceId = process.WorkspaceId,
            SupplierCompanyId = quote.CompanyId,
            CurrencyId = quote.LineItems.FirstOrDefault()?.UnitPrice.CurrencyId ?? process.CurrencyId,
            CreatedByUserId = _currentUser.UserId,
        };
        int sort = 0;
        foreach (RfqQuoteLineItem line in quote.LineItems.OrderBy(l => l.RfqLineItem.SortOrder))
        {
            order.Lines.Add(PurchaseOrderRules.Line(line.RfqLineItemId, line.RfqLineItem.Description, line.RfqLineItem.Quantity,
                line.RfqLineItem.UnitOfMeasure, line.UnitPrice.Amount, sort++));
        }
        order.TotalAmount = order.Lines.Sum(l => l.LineTotal);
        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return [order.Id];
    }
}

// ---------------------------------------------------------------- Liste / detay

public sealed record GetPurchaseOrdersQuery(
    string? Status, string? Search, string? SourceKind, Guid? SourceId, Guid? FirmId, Guid? WorkspaceId, int Page, int PageSize)
    : IRequest<PagedResult<PurchaseOrderListItemDto>>;

public sealed class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _scopes;

    public GetPurchaseOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopes)
    {
        _context = context;
        _currentUser = currentUser;
        _scopes = scopes;
    }

    public async Task<PagedResult<PurchaseOrderListItemDto>> Handle(GetPurchaseOrdersQuery r, CancellationToken cancellationToken)
    {
        IQueryable<PurchaseOrder> query = _context.PurchaseOrders.AsNoTracking();
        bool isInternal = _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer);
        if (isInternal)
        {
            DataScope scope = await _scopes.GetAsync(cancellationToken);
            if (!scope.IsUnrestricted)
            {
                List<Guid> workspaceIds = scope.WorkspaceIds.ToList();
                List<Guid> firmIds = scope.FirmIds.ToList();
                query = query.Where(o => o.WorkspaceId != null ? workspaceIds.Contains(o.WorkspaceId.Value) : firmIds.Contains(o.OwnerCompanyId));
            }
            if (r.WorkspaceId is Guid ws) query = query.Where(o => o.WorkspaceId == ws);
            else if (r.FirmId is Guid firm) query = query.Where(o => o.OwnerCompanyId == firm);
        }
        else
        {
            Guid companyId = _currentUser.CompanyId ?? Guid.Empty;
            query = query.Where(o => o.SupplierCompanyId == companyId && o.Status != PurchaseOrderStatus.Draft);
        }

        if (Enum.TryParse(r.Status, true, out PurchaseOrderStatus status)) query = query.Where(o => o.Status == status);
        if (Enum.TryParse(r.SourceKind, true, out ProcessKind kind)) query = query.Where(o => o.SourceKind == kind);
        if (r.SourceId is Guid sourceId) query = query.Where(o => o.SourceId == sourceId);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            string term = r.Search.Trim().ToLowerInvariant();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(term) || o.SourceTitle.ToLower().Contains(term)
                || o.SupplierCompany.Name.ToLower().Contains(term));
        }

        int total = await query.CountAsync(cancellationToken);
        int page = Math.Max(1, r.Page);
        int pageSize = r.PageSize is < 1 or > 100 ? 20 : r.PageSize;
        List<PurchaseOrderListItemDto> items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new PurchaseOrderListItemDto(
                o.Id, o.OrderNumber, o.SourceKind.ToString(), o.SourceId, o.SourceTitle, o.SupplierCompanyId, o.SupplierCompany.Name,
                o.OwnerCompany.Name, o.Currency.Code, o.TotalAmount, o.Status.ToString(), o.CreatedAt, o.SentAt, o.ConfirmedAt, o.Lines.Count))
            .ToListAsync(cancellationToken);
        return new PagedResult<PurchaseOrderListItemDto>(items, total, page, pageSize);
    }
}

public sealed record GetPurchaseOrderQuery(Guid Id) : IRequest<PurchaseOrderDetailDto>;

public sealed class GetPurchaseOrderQueryHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _scopes;

    public GetPurchaseOrderQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopes)
    {
        _context = context;
        _currentUser = currentUser;
        _scopes = scopes;
    }

    public async Task<PurchaseOrderDetailDto> Handle(GetPurchaseOrderQuery r, CancellationToken cancellationToken)
    {
        PurchaseOrder o = await _context.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines).Include(x => x.OwnerCompany).Include(x => x.SupplierCompany).Include(x => x.Currency)
            .FirstOrDefaultAsync(x => x.Id == r.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrder), r.Id);
        await PurchaseOrderRules.EnsureVisibleAsync(o, _currentUser, _scopes, cancellationToken);

        return new PurchaseOrderDetailDto(
            o.Id, o.OrderNumber, o.SourceKind.ToString(), o.SourceId, o.SourceTitle, o.OwnerCompanyId, o.OwnerCompany.Name,
            o.SupplierCompanyId, o.SupplierCompany.Name, o.Currency.Code, o.TotalAmount, o.Status.ToString(), o.Notes, o.DeliveryDate,
            o.SupplierNote, o.CreatedAt, o.SentAt, o.ConfirmedAt, o.CancelledAt,
            o.Lines.OrderBy(l => l.SortOrder)
                .Select(l => new PurchaseOrderLineDto(l.Id, l.Description, l.Quantity, l.UnitOfMeasure.ToString(), l.UnitPrice, l.LineTotal))
                .ToList());
    }
}

// ---------------------------------------------------------------- Taslak düzenleme, gönderme, onay, iptal

[RequiresPermission(PermissionCodes.OrdersManage)]
public sealed record UpdatePurchaseOrderCommand(Guid Id, string? Notes, DateTime? DeliveryDate) : IRequest;

[RequiresPermission(PermissionCodes.OrdersManage)]
public sealed record SendPurchaseOrderCommand(Guid Id) : IRequest;

[RequiresPermission(PermissionCodes.OrdersManage)]
public sealed record CancelPurchaseOrderCommand(Guid Id, string? Reason) : IRequest;

/// <summary>Tedarikçi gönderilen siparişi onaylar (isteğe bağlı not: teslim planı vb.).</summary>
public sealed record ConfirmPurchaseOrderCommand(Guid Id, string? Note) : IRequest;

public sealed class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator() => RuleFor(x => x.Notes).MaximumLength(2000);
}

public sealed class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}

public sealed class ConfirmPurchaseOrderCommandValidator : AbstractValidator<ConfirmPurchaseOrderCommand>
{
    public ConfirmPurchaseOrderCommandValidator() => RuleFor(x => x.Note).MaximumLength(1000);
}

public sealed class PurchaseOrderStatusHandlers :
    IRequestHandler<UpdatePurchaseOrderCommand>,
    IRequestHandler<SendPurchaseOrderCommand>,
    IRequestHandler<CancelPurchaseOrderCommand>,
    IRequestHandler<ConfirmPurchaseOrderCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _scopes;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _realtime;
    private readonly InternalNotificationAudience _audience;

    public PurchaseOrderStatusHandlers(
        IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopes, IDateTime dateTime,
        INotificationRealtimeService realtime, InternalNotificationAudience audience)
    {
        _context = context;
        _currentUser = currentUser;
        _scopes = scopes;
        _dateTime = dateTime;
        _realtime = realtime;
        _audience = audience;
    }

    private async Task<PurchaseOrder> LoadAsync(Guid id, CancellationToken ct)
    {
        PurchaseOrder order = await _context.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);
        await PurchaseOrderRules.EnsureVisibleAsync(order, _currentUser, _scopes, ct);
        return order;
    }

    public async Task Handle(UpdatePurchaseOrderCommand r, CancellationToken cancellationToken)
    {
        PurchaseOrder order = await LoadAsync(r.Id, cancellationToken);
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new ConflictException("Yalnızca taslak sipariş düzenlenebilir.", ErrorCodes.InvalidStatusTransition);
        order.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
        order.DeliveryDate = r.DeliveryDate is DateTime d ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc) : null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(SendPurchaseOrderCommand r, CancellationToken cancellationToken)
    {
        PurchaseOrder order = await LoadAsync(r.Id, cancellationToken);
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new ConflictException("Yalnızca taslak sipariş gönderilebilir.", ErrorCodes.InvalidStatusTransition);
        order.Status = PurchaseOrderStatus.Sent;
        order.SentAt = _dateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, order.SupplierCompanyId, NotificationType.PurchaseOrder,
            "Yeni Sipariş", $"{order.OrderNumber} numaralı sipariş size gönderildi (\"{order.SourceTitle}\"). Lütfen inceleyip onaylayın.",
            PurchaseOrderRules.RelatedType, order.Id, cancellationToken);
    }

    public async Task Handle(CancelPurchaseOrderCommand r, CancellationToken cancellationToken)
    {
        PurchaseOrder order = await LoadAsync(r.Id, cancellationToken);
        if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Sent))
            throw new ConflictException("Onaylanmış veya iptal edilmiş sipariş iptal edilemez.", ErrorCodes.InvalidStatusTransition);
        bool wasSent = order.Status == PurchaseOrderStatus.Sent;
        order.Status = PurchaseOrderStatus.Cancelled;
        order.CancelledAt = _dateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        if (wasSent)
        {
            string body = $"{order.OrderNumber} numaralı sipariş iptal edildi.";
            if (!string.IsNullOrWhiteSpace(r.Reason)) body += $" Gerekçe: {r.Reason.Trim()}";
            await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, order.SupplierCompanyId, NotificationType.PurchaseOrder,
                "Sipariş İptal Edildi", body, PurchaseOrderRules.RelatedType, order.Id, cancellationToken);
        }
    }

    public async Task Handle(ConfirmPurchaseOrderCommand r, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(Roles.Supplier))
            throw new ForbiddenAccessException("Siparişi yalnızca tedarikçi onaylayabilir.");
        PurchaseOrder order = await LoadAsync(r.Id, cancellationToken);
        if (order.Status != PurchaseOrderStatus.Sent)
            throw new ConflictException("Yalnızca gönderilmiş sipariş onaylanabilir.", ErrorCodes.InvalidStatusTransition);
        order.Status = PurchaseOrderStatus.Confirmed;
        order.ConfirmedAt = _dateTime.UtcNow;
        order.SupplierNote = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        string supplier = await _context.Companies.Where(c => c.Id == order.SupplierCompanyId).Select(c => c.Name).FirstAsync(cancellationToken);
        IReadOnlyList<Guid> audience = await _audience.ForScopeAsync(order.WorkspaceId, order.OwnerCompanyId, order.CreatedByUserId, null, cancellationToken);
        await NotificationCreator.CreateForInternalUsersAsync(_context, _realtime, _dateTime, audience, NotificationType.PurchaseOrder,
            "Sipariş Onaylandı", $"{supplier}, {order.OrderNumber} numaralı siparişi onayladı.", PurchaseOrderRules.RelatedType, order.Id, cancellationToken);
    }
}
