using FluentValidation;

namespace ProcureLive.Application.Catalog.Products.Commands.SetProduct3dModel;

public sealed class SetProduct3dModelCommandValidator : AbstractValidator<SetProduct3dModelCommand>
{
    public SetProduct3dModelCommandValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("3D model adresi boş olamaz.")
            .MaximumLength(1000).WithMessage("3D model adresi en fazla 1000 karakter olabilir.")
            .Must(BeAValidModelUrl).WithMessage("Geçerli bir 3D model adresi girin (.glb veya .gltf uzantılı).");
    }

    private static bool BeAValidModelUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        string path = uri.AbsolutePath.ToLowerInvariant();
        return path.EndsWith(".glb") || path.EndsWith(".gltf");
    }
}
