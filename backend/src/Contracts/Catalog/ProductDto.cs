using ProcureLive.Contracts.Common;

namespace ProcureLive.Contracts.Catalog;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    string UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId,
    bool IsActive,
    Guid? WorkspaceId,
    string? WorkspaceName,
    string? Model3dUrl,
    string? Model3dSource,
    IReadOnlyCollection<ProductImageDto> Images);

public sealed record ProductImageDto(Guid Id, string Url, bool IsPrimary, int SortOrder);

public sealed record SetProduct3dModelRequest(string Url);

public sealed record AssignProductWorkspaceRequest(Guid? WorkspaceId);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    string UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId,
    Guid? WorkspaceId);

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    Guid CategoryId,
    string UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId,
    bool IsActive);

public sealed record AddProductImageRequest(string Url, bool IsPrimary, int SortOrder);

public sealed record ProductListFilter(
    string? SearchTerm,
    Guid? CategoryId,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20) : IPagedRequest;
