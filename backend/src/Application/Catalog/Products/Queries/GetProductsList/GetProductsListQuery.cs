using MediatR;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Application.Catalog.Products.Queries.GetProductsList;

public sealed record GetProductsListQuery(
    string? SearchTerm,
    Guid? CategoryId,
    bool? IsActive,
    Guid? WorkspaceId,
    int Page,
    int PageSize,
    Guid? FirmId = null) : IRequest<PagedResult<ProductDto>>;
