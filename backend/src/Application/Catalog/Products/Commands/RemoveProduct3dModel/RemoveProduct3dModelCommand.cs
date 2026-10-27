using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.RemoveProduct3dModel;

public sealed record RemoveProduct3dModelCommand(Guid ProductId) : IRequest, IProductScopedRequest;
