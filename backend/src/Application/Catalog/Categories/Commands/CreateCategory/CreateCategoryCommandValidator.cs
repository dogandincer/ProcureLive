using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Catalog.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .MustAsync(async (code, cancellationToken) =>
                !await context.Categories.AnyAsync(c => c.Code == code, cancellationToken))
            .WithMessage("Bu koda sahip bir kategori zaten mevcut.");

        RuleFor(x => x.ParentCategoryId)
            .MustAsync(async (parentId, cancellationToken) =>
                parentId is null || await context.Categories.AnyAsync(c => c.Id == parentId, cancellationToken))
            .WithMessage("Üst kategori bulunamadı.");
    }
}
