using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Draft;

/// <summary>
/// Taslak düzenleme: başlık/tarih/şartlar ve kalemler yalnızca Taslak'ta değişir (yayınlanmış ihalede tedarikçiler
/// koşulları görmüştür; değişiklik için önce taslağa çekilir). Ekler ve yanıt verilmemiş davetler Planlandı'da da düzenlenebilir.
/// </summary>
internal static class AuctionEditRules
{
    public static async Task<Auction> LoadAsync(IApplicationDbContext context, Guid auctionId, CancellationToken ct, params AuctionStatus[] allowed)
    {
        Auction auction = await context.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId, ct)
            ?? throw new NotFoundException(nameof(Auction), auctionId);
        if (!allowed.Contains(auction.Status))
        {
            throw new ConflictException(allowed.Length == 1
                ? "Bu değişiklik yalnızca taslak ihalede yapılabilir. Yayınlanmış ihaleyi önce taslağa çekin."
                : "Bu değişiklik yalnızca taslak veya planlanmış ihalede yapılabilir.");
        }
        return auction;
    }
}

// ---------------------------------------------------------------- Başlık, tarihler, şartlar

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record UpdateAuctionDraftCommand(
    Guid AuctionId,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    string? PaymentTerm,
    string? DeliveryTerm,
    decimal? ReservePrice,
    bool AllowPartialBids,
    bool AllowAlternativeProducts,
    bool IsListedForPlus,
    int ExtensionRuleMinutes,
    int? ExtensionDurationMinutes,
    int? MaxExtensionMinutes) : IRequest, IAuctionScopedRequest;

public sealed class UpdateAuctionDraftCommandValidator : AbstractValidator<UpdateAuctionDraftCommand>
{
    public UpdateAuctionDraftCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt).WithMessage("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
        RuleFor(x => x.PaymentTerm).Must(v => v is null || Enum.TryParse<PaymentTerm>(v, out _)).WithMessage("Geçersiz ödeme vadesi.");
        RuleFor(x => x.DeliveryTerm).Must(v => v is null || Enum.TryParse<DeliveryTerm>(v, out _)).WithMessage("Geçersiz teslim şekli.");
        RuleFor(x => x.ReservePrice).GreaterThan(0).When(x => x.ReservePrice is not null);
        RuleFor(x => x.ExtensionRuleMinutes).InclusiveBetween(0, 120);
    }
}

public sealed class UpdateAuctionDraftCommandHandler : IRequestHandler<UpdateAuctionDraftCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateAuctionDraftCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateAuctionDraftCommand r, CancellationToken cancellationToken)
    {
        Auction a = await AuctionEditRules.LoadAsync(_context, r.AuctionId, cancellationToken, AuctionStatus.Draft);
        a.Title = r.Title.Trim();
        a.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        a.StartAt = r.StartAt;
        a.EndAt = r.EndAt;
        a.PaymentTerm = r.PaymentTerm is null ? null : Enum.Parse<PaymentTerm>(r.PaymentTerm);
        a.DeliveryTerm = r.DeliveryTerm is null ? null : Enum.Parse<DeliveryTerm>(r.DeliveryTerm);
        a.ReservePrice = r.ReservePrice;
        a.AllowPartialBids = r.AllowPartialBids;
        a.AllowAlternativeProducts = r.AllowAlternativeProducts;
        a.IsListedForPlus = r.IsListedForPlus;
        a.ExtensionRuleMinutes = r.ExtensionRuleMinutes;
        a.ExtensionDurationMinutes = r.ExtensionDurationMinutes;
        a.MaxExtensionMinutes = r.MaxExtensionMinutes;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// ---------------------------------------------------------------- Kalem düzenle / sil

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record UpdateAuctionItemCommand(
    Guid AuctionId,
    Guid ItemId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? TargetPrice,
    DateTime? RequestedDeliveryDate,
    string? Specification) : IRequest, IAuctionScopedRequest;

public sealed class UpdateAuctionItemCommandValidator : AbstractValidator<UpdateAuctionItemCommand>
{
    public UpdateAuctionItemCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitOfMeasure).Must(v => Enum.TryParse<UnitOfMeasure>(v, out _)).WithMessage("Geçersiz birim.");
        RuleFor(x => x.TargetPrice).GreaterThan(0).When(x => x.TargetPrice is not null);
        RuleFor(x => x.Specification).MaximumLength(4000);
    }
}

public sealed class UpdateAuctionItemCommandHandler : IRequestHandler<UpdateAuctionItemCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateAuctionItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateAuctionItemCommand r, CancellationToken cancellationToken)
    {
        await AuctionEditRules.LoadAsync(_context, r.AuctionId, cancellationToken, AuctionStatus.Draft);
        AuctionItem item = await _context.AuctionItems.FirstOrDefaultAsync(i => i.Id == r.ItemId && i.AuctionId == r.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(AuctionItem), r.ItemId);
        item.Description = r.Description.Trim();
        item.Quantity = r.Quantity;
        item.UnitOfMeasure = Enum.Parse<UnitOfMeasure>(r.UnitOfMeasure);
        item.TargetPrice = r.TargetPrice;
        item.RequestedDeliveryDate = r.RequestedDeliveryDate;
        item.Specification = string.IsNullOrWhiteSpace(r.Specification) ? null : r.Specification.Trim();
        await _context.SaveChangesAsync(cancellationToken);
    }
}

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record DeleteAuctionItemCommand(Guid AuctionId, Guid ItemId) : IRequest, IAuctionScopedRequest;

public sealed class DeleteAuctionItemCommandHandler : IRequestHandler<DeleteAuctionItemCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteAuctionItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteAuctionItemCommand r, CancellationToken cancellationToken)
    {
        await AuctionEditRules.LoadAsync(_context, r.AuctionId, cancellationToken, AuctionStatus.Draft);
        List<AuctionItem> items = await _context.AuctionItems.Where(i => i.AuctionId == r.AuctionId).ToListAsync(cancellationToken);
        AuctionItem item = items.FirstOrDefault(i => i.Id == r.ItemId) ?? throw new NotFoundException(nameof(AuctionItem), r.ItemId);
        if (items.Count == 1)
            throw new ConflictException("İhalede en az bir kalem kalmalıdır.");
        _context.AuctionItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// ---------------------------------------------------------------- Ekler

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record AddAuctionAttachmentCommand(Guid AuctionId, string FileName, string Url, string? ContentType, long SizeBytes)
    : IRequest<Guid>, IAuctionScopedRequest;

public sealed class AddAuctionAttachmentCommandValidator : AbstractValidator<AddAuctionAttachmentCommand>
{
    public AddAuctionAttachmentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.SizeBytes).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddAuctionAttachmentCommandHandler : IRequestHandler<AddAuctionAttachmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddAuctionAttachmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(AddAuctionAttachmentCommand r, CancellationToken cancellationToken)
    {
        await AuctionEditRules.LoadAsync(_context, r.AuctionId, cancellationToken, AuctionStatus.Draft, AuctionStatus.Scheduled);
        var attachment = new AuctionAttachment { AuctionId = r.AuctionId, FileName = r.FileName, Url = r.Url, ContentType = r.ContentType, SizeBytes = r.SizeBytes };
        _context.AuctionAttachments.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken);
        return attachment.Id;
    }
}

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record DeleteAuctionAttachmentCommand(Guid AuctionId, Guid AttachmentId) : IRequest, IAuctionScopedRequest;

public sealed class DeleteAuctionAttachmentCommandHandler : IRequestHandler<DeleteAuctionAttachmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;

    public DeleteAuctionAttachmentCommandHandler(IApplicationDbContext context, IFileStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task Handle(DeleteAuctionAttachmentCommand r, CancellationToken cancellationToken)
    {
        await AuctionEditRules.LoadAsync(_context, r.AuctionId, cancellationToken, AuctionStatus.Draft, AuctionStatus.Scheduled);
        AuctionAttachment att = await _context.AuctionAttachments.FirstOrDefaultAsync(a => a.Id == r.AttachmentId && a.AuctionId == r.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(AuctionAttachment), r.AttachmentId);
        _context.AuctionAttachments.Remove(att);
        await _context.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(att.Url, cancellationToken); // yalnızca /uploads altındaysa siler
    }
}

// ---------------------------------------------------------------- Davet geri alma

/// <summary>Yanıt verilmemiş davet geri alınır; o cari için kullanılmamış davet linkleri de geçersizleşir.</summary>
[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record RevokeAuctionInvitationCommand(Guid AuctionId, Guid InvitationId) : IRequest, IAuctionScopedRequest;

public sealed class RevokeAuctionInvitationCommandHandler : IRequestHandler<RevokeAuctionInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public RevokeAuctionInvitationCommandHandler(IApplicationDbContext context, INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(RevokeAuctionInvitationCommand r, CancellationToken cancellationToken)
    {
        Auction auction = await AuctionEditRules.LoadAsync(_context, r.AuctionId, cancellationToken, AuctionStatus.Draft, AuctionStatus.Scheduled);
        AuctionInvitation inv = await _context.AuctionInvitations.FirstOrDefaultAsync(i => i.Id == r.InvitationId && i.AuctionId == r.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(AuctionInvitation), r.InvitationId);
        if (inv.Status != InvitationStatus.Invited)
            throw new ConflictException("Yalnızca yanıt verilmemiş davet geri alınabilir.");

        DateTime now = _dateTime.UtcNow;
        foreach (InvitationToken token in await _context.InvitationTokens
                     .Where(t => t.AuctionId == r.AuctionId && t.CompanyId == inv.CompanyId && t.UsedAt == null && t.ExpiresAt > now)
                     .ToListAsync(cancellationToken))
        {
            token.ExpiresAt = now;
        }

        _context.AuctionInvitations.Remove(inv);
        await _context.SaveChangesAsync(cancellationToken);

        await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, inv.CompanyId, NotificationType.System,
            "Davet Geri Alındı", $"\"{auction.Title}\" ihalesine davetiniz geri alındı.", null, null, cancellationToken);
    }
}
