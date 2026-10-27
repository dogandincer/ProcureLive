using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Queries.GetProductsList;

public sealed class GetProductsListQueryHandler : IRequestHandler<GetProductsListQuery, PagedResult<ProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsListQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Product> query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string term = request.SearchTerm.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
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
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, page, pageSize);
    }
}
