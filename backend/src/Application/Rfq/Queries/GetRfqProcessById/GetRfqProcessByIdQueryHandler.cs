using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Queries.GetRfqProcessById;

public sealed class GetRfqProcessByIdQueryHandler : IRequestHandler<GetRfqProcessByIdQuery, RfqProcessDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISupplierAccessService _accessService;

    public GetRfqProcessByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISupplierAccessService accessService)
    {
        _context = context;
        _currentUser = currentUser;
        _accessService = accessService;
    }

    public async Task<RfqProcessDetailDto> Handle(GetRfqProcessByIdQuery request, CancellationToken cancellationToken)
    {
        RfqProcess? process = await _context.RfqProcesses
            .Include(p => p.OwnerCompany)
            .Include(p => p.Currency)
            .Include(p => p.LineItems)
            .Include(p => p.Invitations).ThenInclude(i => i.Company)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfqProcess), request.Id);
        }

        List<RfqLineItemDto> lineItems = process.LineItems
            .OrderBy(l => l.SortOrder)
            .Select(l => new RfqLineItemDto(l.Id, l.ProductId, l.Description, l.Quantity, l.UnitOfMeasure.ToString(), l.SortOrder))
            .ToList();

        bool isInternalUser = _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer);
        Guid? viewerCompanyId = _currentUser.CompanyId;

        // Tedarikçi rakip firmaların kimliğini görmez: yalnızca kendi davet kaydı döner.
        List<RfqInvitationDto> invitations = process.Invitations
            .Where(i => isInternalUser || i.CompanyId == viewerCompanyId)
            .Select(i => new RfqInvitationDto(i.Id, i.CompanyId, i.Company.Name, i.Status.ToString(), i.SentAt, i.RespondedAt))
            .ToList();

        bool isOpen = ProcessParticipationRules.IsOpen(process.Status);

        if (isInternalUser || viewerCompanyId is not Guid companyId)
        {
            return new RfqProcessDetailDto(
                process.Id, process.Title, process.OwnerCompanyId, process.OwnerCompany.Name,
                process.CurrencyId, process.Currency.Code, process.Status.ToString(), process.DueDate,
                process.WinningQuoteId, lineItems, invitations, IsOpen: isOpen);
        }

        SupplierAccessContext access = await _accessService.GetContextAsync(companyId, cancellationToken);
        InvitationStatus? myStatus = process.Invitations.FirstOrDefault(i => i.CompanyId == companyId)?.Status;
        AuctionAccessLevel level = ProcessParticipationRules.Evaluate(access, myStatus);

        // Kota dolu ve davet kabul edilmemiş: kalemler sunucuda düşürülür, arayüz "Plus alınız" gösterir.
        if (level != AuctionAccessLevel.Full)
        {
            return new RfqProcessDetailDto(
                process.Id, process.Title, process.OwnerCompanyId, process.OwnerCompany.Name,
                process.CurrencyId, process.Currency.Code, process.Status.ToString(), process.DueDate,
                null, Array.Empty<RfqLineItemDto>(), invitations, level.ToString(), isOpen);
        }

        bool hasSubmitted = await _context.RfqQuotes.AnyAsync(
            q => q.RfqProcessId == process.Id && q.CompanyId == companyId && q.Status == RfqQuoteStatus.Submitted, cancellationToken);

        return new RfqProcessDetailDto(
            process.Id, process.Title, process.OwnerCompanyId, process.OwnerCompany.Name,
            process.CurrencyId, process.Currency.Code, process.Status.ToString(), process.DueDate,
            process.WinningQuoteId, lineItems, invitations, level.ToString(), isOpen, hasSubmitted,
            ProcessParticipationRules.CanSubmit(access, process.Status, myStatus, hasSubmitted));
    }
}
