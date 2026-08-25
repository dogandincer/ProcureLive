using MediatR;

namespace ProcureLive.Application.Catalog.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string Code, Guid? ParentCategoryId) : IRequest<Guid>;
