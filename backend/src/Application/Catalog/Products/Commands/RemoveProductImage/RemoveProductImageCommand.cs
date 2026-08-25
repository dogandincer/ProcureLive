using MediatR;

namespace ProcureLive.Application.Catalog.Products.Commands.RemoveProductImage;

public sealed record RemoveProductImageCommand(Guid ProductId, Guid ImageId) : IRequest;
