using MediatR;
using ProcureLive.Contracts.Catalog;

namespace ProcureLive.Application.Catalog.Categories.Queries.GetCategoriesTree;

public sealed record GetCategoriesTreeQuery : IRequest<IReadOnlyCollection<CategoryDto>>;
