using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Invitations;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Invitations.Commands;

public sealed record CreateInviteLinkCommand(Guid AuctionId, Guid CompanyId, string? Email, int? ValidDays) : IRequest<InviteLinkDto>, IAuctionScopedRequest;

public sealed class CreateInviteLinkCommandValidator : AbstractValidator<CreateInviteLinkCommand>
{
    public CreateInviteLinkCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi girin.");
        RuleFor(x => x.ValidDays).InclusiveBetween(1, InviteRules.MaxValidDays).When(x => x.ValidDays.HasValue);
    }
}

public sealed class CreateInviteLinkCommandHandler : IRequestHandler<CreateInviteLinkCommand, InviteLinkDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public CreateInviteLinkCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<InviteLinkDto> Handle(CreateInviteLinkCommand request, CancellationToken cancellationToken)
    {
        Auction auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), request.AuctionId);

        if (InviteRules.IsAuctionClosed(auction.Status))
            throw new ConflictException("Bu ihale artık katılıma kapalı.", ErrorCodes.AuctionClosed);

        Company company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        if (company.CompanyType != CompanyType.Supplier)
            throw new ConflictException("Davet linki yalnızca tedarikçi carilere oluşturulabilir.");

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        await InviteRules.EnsureInvitedAsync(_context, auction.Id, company.Id, userId, now, cancellationToken);

        var token = new InvitationToken
        {
            Token = InviteRules.NewToken(),
            AuctionId = auction.Id,
            CompanyId = company.Id,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            ExpiresAt = now.AddDays(request.ValidDays ?? InviteRules.DefaultValidDays),
            CreatedByUserId = userId,
        };

        _context.InvitationTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);

        return InviteRules.ToDto(token, company.Name, now);
    }
}
