using FluentValidation;

namespace ProcureLive.Application.Catalog.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x)
            .Must(x => x.ParentCategoryId != x.Id)
            .WithMessage("Bir kategori kendi üst kategorisi olamaz.");
    }
}
