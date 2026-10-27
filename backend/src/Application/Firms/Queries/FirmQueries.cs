using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Firms;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Firms.Queries;

public sealed record GetFirmsQuery : IRequest<List<FirmDto>>;

public sealed class GetFirmsQueryHandler : IRequestHandler<GetFirmsQuery, List<FirmDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFirmsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<FirmDto>> Handle(GetFirmsQuery request, CancellationToken cancellationToken) =>
        FirmRules.ToDtosAsync(_context, FirmRules.Firms(_context), cancellationToken);
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
