using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Subscriptions;

namespace ProcureLive.Application.Companies.Settings;

/// <summary>
/// Firma gizlilik/listeleme ayarları. CompanyId null → oturumdaki kullanıcının carisi.
/// Tedarikçi yalnızca kendi carisini görebilir/değiştirebilir; iç kullanıcılar (Admin/Buyer) tüm carileri.
/// </summary>
public sealed record GetCompanySettingsQuery(Guid? CompanyId) : IRequest<CompanySettingsDto>;

public sealed record UpdateCompanySettingsCommand(Guid? CompanyId, string? LogoUrl, bool ShowContactInfo, bool ListTendersForPlus)
    : IRequest<CompanySettingsDto>;

public sealed class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.LogoUrl)
            .MaximumLength(1000).WithMessage("Logo adresi en fazla 1000 karakter olabilir.")
            .Must(BeAnHttpUrl).WithMessage("Geçerli bir logo adresi girin (http/https).")
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));
    }

    private static bool BeAnHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

internal static class CompanySettingsAccess
{
    public static Guid ResolveCompanyId(ICurrentUserService currentUser, Guid? requested)
    {
        bool isInternalUser = currentUser.IsInRole(Roles.Admin) || currentUser.IsInRole(Roles.Buyer);
        Guid? own = currentUser.CompanyId;

        if (requested is null)
        {
            return own ?? throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        if (!isInternalUser && requested != own)
        {
            throw new ForbiddenAccessException();
        }

        return requested.Value;
    }

    public static async Task<CompanySettingsDto> ToDtoAsync(Company c, ISubscriptionService subscriptions, CancellationToken ct)
    {
        CompanySubscription? plus = await subscriptions.GetActivePlusAsync(c.Id, ct);
        return new CompanySettingsDto(
            c.Id, c.Name, c.CompanyType.ToString(), c.LogoUrl, c.ShowContactInfo, c.ListTendersForPlus,
            plus is not null, plus?.ExpiresAt);
    }
}

public sealed class GetCompanySettingsQueryHandler : IRequestHandler<GetCompanySettingsQuery, CompanySettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;

    public GetCompanySettingsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, ISubscriptionService subscriptionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
    }

    public async Task<CompanySettingsDto> Handle(GetCompanySettingsQuery request, CancellationToken cancellationToken)
    {
        Guid companyId = CompanySettingsAccess.ResolveCompanyId(_currentUserService, request.CompanyId);

        Company company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), companyId);

        return await CompanySettingsAccess.ToDtoAsync(company, _subscriptionService, cancellationToken);
    }
}

public sealed class UpdateCompanySettingsCommandHandler : IRequestHandler<UpdateCompanySettingsCommand, CompanySettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;

    public UpdateCompanySettingsCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, ISubscriptionService subscriptionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
    }

    public async Task<CompanySettingsDto> Handle(UpdateCompanySettingsCommand request, CancellationToken cancellationToken)
    {
        Guid companyId = CompanySettingsAccess.ResolveCompanyId(_currentUserService, request.CompanyId);

        Company company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), companyId);

        company.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        company.ShowContactInfo = request.ShowContactInfo;
        company.ListTendersForPlus = request.ListTendersForPlus;

        await _context.SaveChangesAsync(cancellationToken);

        return await CompanySettingsAccess.ToDtoAsync(company, _subscriptionService, cancellationToken);
    }
}
