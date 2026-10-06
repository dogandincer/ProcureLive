using MediatR;

namespace ProcureLive.Application.Catalog.Products.Commands.RemoveProduct3dModel;

public sealed record RemoveProduct3dModelCommand(Guid ProductId) : IRequest;
