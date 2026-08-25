using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Application.Rfq.Queries.GetRfqProcessById;

public sealed class GetRfqProcessByIdQueryHandler : IRequestHandler<GetRfqProcessByIdQuery, RfqProcessDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetRfqProcessByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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

        List<RfqInvitationDto> invitations = process.Invitations
            .Select(i => new RfqInvitationDto(i.Id, i.CompanyId, i.Company.Name, i.Status.ToString(), i.SentAt, i.RespondedAt))
            .ToList();

        return new RfqProcessDetailDto(
            process.Id, process.Title, process.OwnerCompanyId, process.OwnerCompany.Name,
            process.CurrencyId, process.Currency.Code, process.Status.ToString(), process.DueDate,
            lineItems, invitations);
    }
}
