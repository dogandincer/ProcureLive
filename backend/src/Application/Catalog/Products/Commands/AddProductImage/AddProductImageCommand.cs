using MediatR;

namespace ProcureLive.Application.Catalog.Products.Commands.AddProductImage;

public sealed record AddProductImageCommand(Guid ProductId, string Url, bool IsPrimary, int SortOrder) : IRequest<Guid>;
