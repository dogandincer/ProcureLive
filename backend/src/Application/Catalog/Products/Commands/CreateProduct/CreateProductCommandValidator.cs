using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Catalog.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(50)
            .MustAsync(async (sku, cancellationToken) =>
                !await context.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
            .WithMessage("Bu SKU'ya sahip bir ürün zaten mevcut.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);

        RuleFor(x => x.CategoryId)
            .MustAsync(async (categoryId, cancellationToken) =>
                await context.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
            .WithMessage("Kategori bulunamadı.");

        RuleFor(x => x.UnitOfMeasure).IsInEnum();

        RuleFor(x => x.DefaultCurrencyId)
            .MustAsync(async (currencyId, cancellationToken) =>
                currencyId is null || await context.Currencies.AnyAsync(c => c.Id == currencyId, cancellationToken))
            .WithMessage("Para birimi bulunamadı.");
    }
}
