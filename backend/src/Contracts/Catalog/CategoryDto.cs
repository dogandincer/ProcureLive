namespace ProcureLive.Contracts.Catalog;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentCategoryId,
    IReadOnlyCollection<CategoryDto> Children);

public sealed record CreateCategoryRequest(string Name, string Code, Guid? ParentCategoryId);

public sealed record UpdateCategoryRequest(string Name, string Code, Guid? ParentCategoryId);
