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
    IReadOnlyCollection<ProductImageDto> Images);

public sealed record ProductImageDto(Guid Id, string Url, bool IsPrimary, int SortOrder);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    string UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId);

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
