using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Notifications.Queries.GetUnreadCount;

public sealed class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationCountQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);
        Guid? companyId = _currentUserService.CompanyId;
        Guid? userId = _currentUserService.UserId;

        return isInternalUser
            ? await _context.Notifications.CountAsync(n => n.CompanyId == null && (n.UserId == null || n.UserId == userId) && !n.IsRead, cancellationToken)
            : await _context.Notifications.CountAsync(n => n.CompanyId == companyId && !n.IsRead, cancellationToken);
    }
}
