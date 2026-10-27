using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Firms;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Firms.Queries;

public sealed record GetFirmsQuery : IRequest<List<FirmDto>>;

public sealed class GetFirmsQueryHandler : IRequestHandler<GetFirmsQuery, List<FirmDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataScopeService _scope;

    public GetFirmsQueryHandler(IApplicationDbContext context, IDataScopeService scope)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<List<FirmDto>> Handle(GetFirmsQuery request, CancellationToken cancellationToken)
    {
        // Satınalmacı yalnızca kendisine işyeri atanmış firmaları görür; Admin hepsini.
        DataScope scope = await _scope.GetAsync(cancellationToken);
        IQueryable<Company> firms = FirmRules.Firms(_context);
        if (!scope.IsUnrestricted)
        {
            List<Guid> ids = scope.FirmIds.ToList();
            firms = firms.Where(c => ids.Contains(c.Id));
        }

        return await FirmRules.ToDtosAsync(_context, firms, cancellationToken);
    }
}

/// <summary>Anonim: giriş ekranı ve sekme için uygulama adı + logo (varsayılan firmadan).</summary>
public sealed record GetBrandingQuery : IRequest<BrandingDto>;

public sealed class GetBrandingQueryHandler : IRequestHandler<GetBrandingQuery, BrandingDto>
{
    private readonly IApplicationDbContext _context;

    public GetBrandingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BrandingDto> Handle(GetBrandingQuery request, CancellationToken cancellationToken)
    {
        Company? firm = await FirmRules.Firms(_context).AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsDefaultFirm, cancellationToken);
        return FirmRules.Branding(firm);
    }
}
