using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.AddProductImage;

public sealed record AddProductImageCommand(Guid ProductId, string Url, bool IsPrimary, int SortOrder) : IRequest<Guid>, IProductScopedRequest;
