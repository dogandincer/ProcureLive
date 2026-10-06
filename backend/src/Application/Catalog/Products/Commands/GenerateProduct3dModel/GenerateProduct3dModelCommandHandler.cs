using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Commands.GenerateProduct3dModel;

/// <summary>
/// Simulated photo-to-3D generation. Real photogrammetry/AI-mesh generation requires an external
/// paid service (e.g. Meshy, Luma, Tripo); like the email sender, this ships a stand-in that keeps
/// the flow end-to-end usable. Swapping in a real provider later only touches this handler.
/// </summary>
public sealed class GenerateProduct3dModelCommandHandler : IRequestHandler<GenerateProduct3dModelCommand, string>
{
    private readonly IApplicationDbContext _context;

    // Public sample models hosted by the model-viewer project (the viewer library used on the client).
    private static readonly string[] DemoModels =
    {
        "https://modelviewer.dev/shared-assets/models/Astronaut.glb",
        "https://modelviewer.dev/shared-assets/models/RobotExpressive.glb",
        "https://modelviewer.dev/shared-assets/models/reflective-sphere.glb",
        "https://modelviewer.dev/shared-assets/models/NeilArmstrong.glb",
        "https://modelviewer.dev/shared-assets/models/shishkebab.glb",
    };

    public GenerateProduct3dModelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(GenerateProduct3dModelCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            throw new NotFoundException(nameof(Product), request.ProductId);

        if (product.Images.Count == 0)
            throw new ConflictException("3D model oluşturmak için en az bir ürün fotoğrafı gereklidir.");

        // Deterministic pick so re-generating the same product yields a stable result.
        int index = Math.Abs(product.Id.GetHashCode()) % DemoModels.Length;
        string modelUrl = DemoModels[index];

        product.Model3dUrl = modelUrl;
        product.Model3dSource = "AiGenerated";

        await _context.SaveChangesAsync(cancellationToken);

        return modelUrl;
    }
}
