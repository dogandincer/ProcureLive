using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Users;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Users;

/// <summary>Tedarikçi carinin giriş kullanıcıları (Cari kartındaki "Kullanıcılar" kartı).</summary>
public sealed record GetCompanyUsersQuery(Guid CompanyId) : IRequest<IReadOnlyList<UserDto>>, ICompanyScopedRequest;

public sealed class GetCompanyUsersQueryHandler : IRequestHandler<GetCompanyUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IIdentityService _identityService;

    public GetCompanyUsersQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(GetCompanyUsersQuery request, CancellationToken cancellationToken)
    {
        PagedResult<ApplicationUserInfo> result = await _identityService.ListUsersAsync(
            null, request.CompanyId, 1, 200, [Roles.Supplier], cancellationToken);

        return result.Items
            .Select(u => new UserDto(u.Id, u.Email, u.FirstName, u.LastName, u.Roles, u.CompanyId, u.IsActive, u.LastLoginAt, u.LockedOutAt))
            .ToList();
    }
}

public sealed record CreateCompanyUserCommand(Guid CompanyId, string Email, string Password, string FirstName, string LastName)
    : IRequest<Guid>, ICompanyScopedRequest;

public sealed class CreateCompanyUserCommandValidator : AbstractValidator<CreateCompanyUserCommand>
{
    public CreateCompanyUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.").MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateCompanyUserCommandHandler : IRequestHandler<CreateCompanyUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public CreateCompanyUserCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<Guid> Handle(CreateCompanyUserCommand request, CancellationToken cancellationToken)
    {
        CompanyType? type = await _context.Companies.Where(c => c.Id == request.CompanyId)
            .Select(c => (CompanyType?)c.CompanyType).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        if (type == CompanyType.Internal)
            throw new ConflictException("Firma kullanıcıları Kullanıcı Tanım ekranından açılır.");

        string email = request.Email.Trim().ToLowerInvariant();
        if (await _identityService.FindByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictException("Bu e-posta ile zaten bir kullanıcı var.", ErrorCodes.AccountExists);

        var result = await _identityService.CreateUserAsync(
            email, request.Password, request.FirstName.Trim(), request.LastName.Trim(), Roles.Supplier, request.CompanyId, cancellationToken);

        if (!result.Succeeded || result.UserId is null)
            throw new ConflictException(string.Join("; ", result.Errors));

        return result.UserId.Value;
    }
}

/// <summary>Carinin ek görünür işyerlerini değiştirir (birincil işyeri ayrı yönetilir).</summary>
public sealed record SetCompanyWorkspacesCommand(Guid CompanyId, IReadOnlyList<Guid> WorkspaceIds) : IRequest, ICompanyScopedRequest;

public sealed class SetCompanyWorkspacesCommandHandler : IRequestHandler<SetCompanyWorkspacesCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataScopeService _scopeService;

    public SetCompanyWorkspacesCommandHandler(IApplicationDbContext context, IDataScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public async Task Handle(SetCompanyWorkspacesCommand request, CancellationToken cancellationToken)
    {
        Company company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        if (company.CompanyType == CompanyType.Internal)
            throw new ConflictException("Firmalar için işyeri görünürlüğü tanımlanmaz.");

        DataScope scope = await _scopeService.GetAsync(cancellationToken);
        await CompanyRules.SetVisibleWorkspacesAsync(_context, company, request.WorkspaceIds, scope, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
