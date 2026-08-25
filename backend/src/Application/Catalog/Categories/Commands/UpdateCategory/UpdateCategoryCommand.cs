using MediatR;

namespace ProcureLive.Application.Catalog.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid Id, string Name, string Code, Guid? ParentCategoryId) : IRequest;
