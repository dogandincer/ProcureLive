using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.GenerateProduct3dModel;

/// <summary>Produces a 3D model from the product's photos. Returns the resulting model URL.</summary>
public sealed record GenerateProduct3dModelCommand(Guid ProductId) : IRequest<string>, IProductScopedRequest;
