using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);

        RuleFor(x => x.Code).MaximumLength(30);

        RuleFor(x => x.TaxNumber)
            .NotEmpty()
            .Length(10, 11)
            .MustAsync(async (taxNumber, cancellationToken) =>
                !await context.Companies.AnyAsync(c => c.TaxNumber == taxNumber, cancellationToken))
            .WithMessage("Bu vergi numarasına sahip cari zaten mevcut.");

        RuleFor(x => x.TaxOffice).NotEmpty().MaximumLength(150);

        RuleFor(x => x.CompanyType).IsInEnum()
            .Must(t => t != CompanyType.Internal)
            .WithMessage("Firma tanımı Firma Tanım ekranından yapılır; Cari ekranı yalnızca tedarikçiler içindir.");

        RuleFor(x => x.Website).MaximumLength(250);

        // Sistem işyeri bazlı: cari oluştururken geçerli bir işyeri zorunlu.
        RuleFor(x => x.WorkspaceId)
            .NotNull().WithMessage("İşyeri seçimi zorunludur. Önce bir işyeri tanımlayın/seçin.")
            .MustAsync(async (id, cancellationToken) =>
                id.HasValue && await context.Workspaces.AnyAsync(w => w.Id == id.Value, cancellationToken))
            .WithMessage("Geçerli bir işyeri seçin.");

        // Kullanıcı bilgisi opsiyonel; e-posta girildiyse şifre ve ad/soyad da zorunlu.
        When(x => !string.IsNullOrWhiteSpace(x.UserEmail), () =>
        {
            RuleFor(x => x.UserEmail).EmailAddress().WithMessage("Geçerli bir kullanıcı e-postası girin.").MaximumLength(200);
            RuleFor(x => x.UserPassword).NotEmpty().MinimumLength(8).WithMessage("Kullanıcı şifresi en az 8 karakter olmalıdır.");
            RuleFor(x => x.UserFirstName).NotEmpty().WithMessage("Kullanıcı adı zorunludur.").MaximumLength(100);
            RuleFor(x => x.UserLastName).NotEmpty().WithMessage("Kullanıcı soyadı zorunludur.").MaximumLength(100);
        });
    }
}
