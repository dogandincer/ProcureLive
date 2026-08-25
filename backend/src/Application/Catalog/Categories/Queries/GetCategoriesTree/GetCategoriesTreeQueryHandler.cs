using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Categories.Queries.GetCategoriesTree;

public sealed class GetCategoriesTreeQueryHandler
    : IRequestHandler<GetCategoriesTreeQuery, IReadOnlyCollection<CategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesTreeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<CategoryDto>> Handle(
        GetCategoriesTreeQuery request,
        CancellationToken cancellationToken)
    {
        List<Category> all = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, List<Category>> childrenByParent = all
            .Where(c => c.ParentCategoryId.HasValue)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<Category> roots = all.Where(c => c.ParentCategoryId is null).ToList();

        return roots.Select(root => Map(root, childrenByParent)).ToList();
    }

    private static CategoryDto Map(Category category, Dictionary<Guid, List<Category>> childrenByParent)
    {
        List<Category> children;
        bool hasChildren = childrenByParent.TryGetValue(category.Id, out children!);

        IReadOnlyCollection<CategoryDto> childDtos = hasChildren
            ? children.Select(c => Map(c, childrenByParent)).ToList()
            : Array.Empty<CategoryDto>();

        return new CategoryDto(category.Id, category.Name, category.Code, category.ParentCategoryId, childDtos);
    }
}
