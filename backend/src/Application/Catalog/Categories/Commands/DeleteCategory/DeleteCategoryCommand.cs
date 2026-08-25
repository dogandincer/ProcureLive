using MediatR;

namespace ProcureLive.Application.Catalog.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest;
