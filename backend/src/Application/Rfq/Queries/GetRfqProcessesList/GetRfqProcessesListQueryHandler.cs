using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Queries.GetRfqProcessesList;

public sealed class GetRfqProcessesListQueryHandler : IRequestHandler<GetRfqProcessesListQuery, PagedResult<RfqProcessDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataScopeService _scope;

    public GetRfqProcessesListQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDataScopeService scope)
    {
        _scope = scope;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<RfqProcessDto>> Handle(GetRfqProcessesListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<RfqProcess> query = _context.RfqProcesses
            .Include(p => p.OwnerCompany)
            .Include(p => p.Currency)
            .Include(p => p.LineItems)
            .Include(p => p.Invitations)
            .Include(p => p.Quotes)
            .AsQueryable();

        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);

        if (!isInternalUser)
        {
            Guid companyId = _currentUserService.CompanyId ?? Guid.Empty;
            query = query.Where(p => p.Invitations.Any(i => i.CompanyId == companyId));
        }
        else
        {
            // Satınalmacı yalnızca yetkili firmalarının süreçlerini görür.
            DataScope scope = await _scope.GetAsync(cancellationToken);
            if (!scope.IsUnrestricted)
            {
                List<Guid> firmIds = scope.FirmIds.ToList();
                query = query.Where(p => firmIds.Contains(p.OwnerCompanyId));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            ProcessStatus status;
            bool parsed = Enum.TryParse(request.Status, true, out status);

            if (parsed)
            {
                query = query.Where(p => p.Status == status);
            }
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<RfqProcess> processes = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<RfqProcessDto> items = processes.Select(p => new RfqProcessDto(
            p.Id, p.Title, p.OwnerCompanyId, p.OwnerCompany.Name, p.CurrencyId, p.Currency.Code,
            p.Status.ToString(), p.DueDate, p.LineItems.Count, p.Invitations.Count, p.Quotes.Count, p.CreatedAt)).ToList();

        return new PagedResult<RfqProcessDto>(items, totalCount, page, pageSize);
    }
}
