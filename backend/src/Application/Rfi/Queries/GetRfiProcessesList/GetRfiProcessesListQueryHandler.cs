using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessesList;

public sealed class GetRfiProcessesListQueryHandler : IRequestHandler<GetRfiProcessesListQuery, PagedResult<RfiProcessDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataScopeService _scope;

    public GetRfiProcessesListQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDataScopeService scope)
    {
        _scope = scope;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<RfiProcessDto>> Handle(GetRfiProcessesListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<RfiProcess> query = _context.RfiProcesses
            .Include(p => p.OwnerCompany)
            .Include(p => p.RfiTemplate)
            .Include(p => p.Invitations)
            .Include(p => p.Responses)
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

        List<RfiProcess> processes = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<RfiProcessDto> items = processes.Select(p => new RfiProcessDto(
            p.Id, p.RfiTemplateId, p.RfiTemplate.Name, p.Title, p.OwnerCompanyId, p.OwnerCompany.Name,
            p.Status.ToString(), p.DueDate, p.Invitations.Count,
            p.Responses.Select(r => r.CompanyId).Distinct().Count(), p.CreatedAt)).ToList();

        return new PagedResult<RfiProcessDto>(items, totalCount, page, pageSize);
    }
}
