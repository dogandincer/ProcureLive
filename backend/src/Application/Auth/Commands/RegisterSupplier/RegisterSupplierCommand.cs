using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Common;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Companies;
using ProcureLive.Contracts.Auth;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;
using RefreshTokenEntity = ProcureLive.Domain.Entities.Identity.RefreshToken;

namespace ProcureLive.Application.Auth.Commands.RegisterSupplier;

/// <summary>
/// Davetsiz (kendi isteğiyle) tedarikçi kaydı: cari "Onay Bekliyor" durumunda ve hiçbir işyerine bağlı olmadan açılır,
/// kullanıcı oturum açmış olarak döner. Bir ihaleye katılmak istediğinde katılım talebi gönderir; cari onayı o ihalenin
/// firması/işyeri tarafından verilir.
/// </summary>
public sealed record RegisterSupplierCommand(
    string CompanyName,
    string TaxNumber,
    string TaxOffice,
    string? Phone,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool AcceptTerms) : IRequest<AuthResultDto>;

public sealed class RegisterSupplierCommandValidator : AbstractValidator<RegisterSupplierCommand>
{
    public RegisterSupplierCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Firma adı zorunludur.").MaximumLength(250);
        RuleFor(x => x.TaxNumber).NotEmpty().Matches("^[0-9]{10,11}$").WithMessage("VKN 10, TCKN 11 haneli olmalıdır.");
        RuleFor(x => x.TaxOffice).NotEmpty().WithMessage("Vergi dairesi zorunludur.").MaximumLength(150);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.").MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
        RuleFor(x => x.AcceptTerms).Equal(true).WithMessage("Devam etmek için KVKK aydınlatma metnini onaylamalısınız.");
    }
}

public sealed class RegisterSupplierCommandHandler : IRequestHandler<RegisterSupplierCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _realtime;

    public RegisterSupplierCommandHandler(
        IApplicationDbContext context, IIdentityService identityService, ITokenService tokenService, IDateTime dateTime,
        INotificationRealtimeService realtime)
    {
        _context = context;
        _identityService = identityService;
        _tokenService = tokenService;
        _dateTime = dateTime;
        _realtime = realtime;
    }

    public async Task<AuthResultDto> Handle(RegisterSupplierCommand request, CancellationToken cancellationToken)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        string taxNumber = request.TaxNumber.Trim();

        if (await _identityService.FindByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictException("Bu e-posta ile zaten bir hesap var. Lütfen giriş yapın.", ErrorCodes.AccountExists);

        if (await _context.Companies.AnyAsync(c => c.TaxNumber == taxNumber, cancellationToken))
            throw new ConflictException(
                "Bu vergi numarasıyla kayıtlı bir firma var. Firmanızın yöneticisinden kullanıcı ya da davet linki isteyin.",
                ErrorCodes.AccountExists);

        DateTime now = _dateTime.UtcNow;

        var company = new Company
        {
            Code = await CompanyRules.NextCodeAsync(_context, cancellationToken),
            Name = request.CompanyName.Trim(),
            TaxNumber = taxNumber,
            TaxOffice = request.TaxOffice.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = email,
            CompanyType = CompanyType.Supplier,
            Status = CompanyStatus.PendingApproval,
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync(cancellationToken);

        var createResult = await _identityService.CreateUserAsync(
            email, request.Password, request.FirstName.Trim(), request.LastName.Trim(), Roles.Supplier, company.Id, cancellationToken);

        if (!createResult.Succeeded || createResult.UserId is null)
        {
            // Kullanıcı açılamadıysa yetim cari bırakılmaz.
            company.IsDeleted = true;
            company.DeletedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            throw new ConflictException(string.Join("; ", createResult.Errors));
        }

        ApplicationUserInfo user = await _identityService.FindByIdAsync(createResult.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("ApplicationUser", createResult.UserId.Value);

        (string accessToken, DateTime expiresAt) = _tokenService.GenerateAccessToken(user);
        string refreshTokenValue = _tokenService.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = now.AddDays(7),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Bağlanmamış kayıt yalnızca Admin'in cari havuzunda görünür; bildirim de yalnızca aktif Admin'lere gider.
        PagedResult<ApplicationUserInfo> admins = await _identityService.ListUsersAsync(null, null, 1, 100, [Roles.Admin], cancellationToken);
        foreach (ApplicationUserInfo adminUser in admins.Items.Where(a => a.IsActive))
        {
            await NotificationCreator.CreateForInternalUserAsync(
                _context, _realtime, _dateTime, adminUser.Id, NotificationType.System,
                "Yeni Tedarikçi Kaydı",
                $"\"{company.Name}\" kendi kaydını oluşturdu (onay bekliyor).",
                nameof(Company), company.Id, cancellationToken);
        }

        return new AuthResultDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.Roles, user.CompanyId, accessToken, expiresAt, refreshTokenValue);
    }
}
