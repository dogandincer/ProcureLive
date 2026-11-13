using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Mappings;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Queries.GetProductsList;

public sealed class GetProductsListQueryHandler : IRequestHandler<GetProductsListQuery, PagedResult<ProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataScopeService _scope;

    public GetProductsListQueryHandler(IApplicationDbContext context, IDataScopeService scope)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Product> query = _context.Products.AsQueryable();

        DataScope scope = await _scope.GetAsync(cancellationToken);
        if (!scope.IsUnrestricted)
        {
            List<Guid> workspaceIds = scope.WorkspaceIds.ToList();
            query = query.Where(p => p.WorkspaceId.HasValue && workspaceIds.Contains(p.WorkspaceId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string term = request.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        if (request.WorkspaceId.HasValue)
        {
            query = query.Where(p => p.WorkspaceId == request.WorkspaceId.Value);
        }
        else if (request.FirmId.HasValue)
        {
            query = query.Where(p => p.Workspace != null && p.Workspace.FirmId == request.FirmId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<ProductDto> items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projections.Product)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, page, pageSize);
    }
}
