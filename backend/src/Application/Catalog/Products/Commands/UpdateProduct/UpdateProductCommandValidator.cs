using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Catalog.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);

        RuleFor(x => x.CategoryId)
            .MustAsync(async (categoryId, cancellationToken) =>
                await context.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
            .WithMessage("Kategori bulunamadı.");

        RuleFor(x => x.UnitOfMeasure).IsInEnum();
    }
}
