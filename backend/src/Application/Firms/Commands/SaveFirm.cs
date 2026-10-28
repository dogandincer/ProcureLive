using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Firms;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Firms.Commands;

/// <summary>Firma oluştur (Id null) veya güncelle. İlk firma otomatik varsayılan olur; varsayılan her zaman tektir.</summary>
public sealed record SaveFirmCommand(Guid? Id, SaveFirmRequest Data) : IRequest<Guid>;

public sealed class SaveFirmCommandValidator : AbstractValidator<SaveFirmCommand>
{
    public SaveFirmCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Data.Code)
            .NotEmpty().WithMessage("Firma kodu zorunludur.")
            .MaximumLength(30)
            .MustAsync(async (cmd, code, ct) =>
                !await context.Companies.AnyAsync(c => c.Code == code.Trim() && c.Id != cmd.Id, ct))
            .WithMessage("Bu kodla bir kayıt zaten mevcut.");
        RuleFor(x => x.Data.Name).NotEmpty().WithMessage("Firma adı zorunludur.").MaximumLength(250);
        RuleFor(x => x.Data.TradeName).MaximumLength(300);
        RuleFor(x => x.Data.ShortName).MaximumLength(60);
        RuleFor(x => x.Data.AppTitleSuffix).MaximumLength(100);
        RuleFor(x => x.Data.Website).MaximumLength(250);
        RuleFor(x => x.Data.TaxOffice).MaximumLength(150);
        RuleFor(x => x.Data.Phone).MaximumLength(30);
        RuleFor(x => x.Data.Phone2).MaximumLength(30);
        RuleFor(x => x.Data.MobilePhone).MaximumLength(30);
        RuleFor(x => x.Data.Fax).MaximumLength(30);
        RuleFor(x => x.Data.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Data.Email))
            .WithMessage("Geçerli bir e-posta adresi girin.");
        RuleFor(x => x.Data.Email2).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Data.Email2))
            .WithMessage("Geçerli bir e-posta adresi girin.");
        RuleFor(x => x.Data.TaxNumber)
            .Matches(@"^\d{10,11}$").WithMessage("Vergi / TC kimlik numarası 10 veya 11 haneli olmalıdır.")
            .MustAsync(async (cmd, tax, ct) =>
                !await context.Companies.AnyAsync(c => c.TaxNumber == tax!.Trim() && c.Id != cmd.Id, ct))
            .WithMessage("Bu vergi numarasına sahip cari zaten mevcut.")
            .When(x => !string.IsNullOrWhiteSpace(x.Data.TaxNumber));
    }
}

public sealed class SaveFirmCommandHandler : IRequestHandler<SaveFirmCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public SaveFirmCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(SaveFirmCommand request, CancellationToken cancellationToken)
    {
        SaveFirmRequest d = request.Data;
        Company firm;

        if (request.Id is Guid id)
        {
            firm = await FirmRules.Firms(_context).Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException("Firm", id);

            if (firm.IsDefaultFirm && !d.IsDefault)
                throw new ConflictException("Varsayılan firma kaldırılamaz; önce başka bir firmayı varsayılan yapın.");
        }
        else
        {
            firm = new Company { CompanyType = CompanyType.Internal, Status = CompanyStatus.Active };
            _context.Companies.Add(firm);
        }

        firm.Code = d.Code.Trim();
        firm.Name = d.Name.Trim();
        firm.TradeName = Clean(d.TradeName);
        firm.ShortName = Clean(d.ShortName);
        firm.Phone = Clean(d.Phone);
        firm.Phone2 = Clean(d.Phone2);
        firm.MobilePhone = Clean(d.MobilePhone);
        firm.Fax = Clean(d.Fax);
        firm.Email = Clean(d.Email);
        firm.Email2 = Clean(d.Email2);
        firm.Website = Clean(d.Website);
        firm.TaxOffice = Clean(d.TaxOffice) ?? string.Empty;
        firm.TaxNumber = Clean(d.TaxNumber) ?? string.Empty;
        firm.AppTitleSuffix = Clean(d.AppTitleSuffix);
        firm.RequirePublishApproval = d.RequirePublishApproval;

        UpsertBillingAddress(firm, d);

        // İlk firma kendiliğinden varsayılan; varsayılan işaretlenirse diğerlerinden kaldırılır (tek varsayılan).
        bool anyOtherDefault = await FirmRules.Firms(_context).AnyAsync(c => c.IsDefaultFirm && c.Id != firm.Id, cancellationToken);
        bool makeDefault = d.IsDefault || !anyOtherDefault;
        if (makeDefault && anyOtherDefault)
        {
            List<Company> others = await FirmRules.Firms(_context).Where(c => c.IsDefaultFirm && c.Id != firm.Id).ToListAsync(cancellationToken);
            others.ForEach(o => o.IsDefaultFirm = false);
        }
        firm.IsDefaultFirm = makeDefault;

        await _context.SaveChangesAsync(cancellationToken);
        return firm.Id;
    }

    private void UpsertBillingAddress(Company firm, SaveFirmRequest d)
    {
        bool any = new[] { d.BillingCity, d.BillingDistrict, d.BillingPostalCode, d.BillingAddressLine }.Any(v => !string.IsNullOrWhiteSpace(v));
        Address? billing = firm.Addresses.Where(a => a.AddressType == AddressType.Billing).OrderBy(a => a.CreatedAt).FirstOrDefault();

        if (billing is null)
        {
            if (!any) return;
            billing = new Address { CompanyId = firm.Id, AddressType = AddressType.Billing };
            // Takip edilen firmaya koleksiyon üzerinden değil DbSet üzerinden eklenir (önceden üretilen Id → UPDATE sanılmasın).
            _context.Addresses.Add(billing);
        }

        billing.Country = Clean(d.BillingCountry) ?? "Türkiye";
        billing.City = Clean(d.BillingCity) ?? string.Empty;
        billing.District = Clean(d.BillingDistrict);
        billing.PostalCode = Clean(d.BillingPostalCode);
        billing.Line1 = Clean(d.BillingAddressLine) ?? string.Empty;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
