using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.SetProduct3dModel;

public sealed record SetProduct3dModelCommand(Guid ProductId, string Url) : IRequest, IProductScopedRequest;
