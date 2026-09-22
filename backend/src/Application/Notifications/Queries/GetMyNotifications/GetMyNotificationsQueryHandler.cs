using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Localization;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Notifications;
using ProcureLive.Domain.Entities.Notifications;

namespace ProcureLive.Application.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);
        Guid? companyId = _currentUserService.CompanyId;

        IQueryable<Notification> query = _context.Notifications.AsQueryable();

        query = isInternalUser
            ? query.Where(n => n.CompanyId == null)
            : query.Where(n => n.CompanyId == companyId);

        if (request.UnreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<Notification> entities = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Title/Body are stored canonically in Turkish and translated here, at read time, based on the
        // CURRENT viewer's language - so switching language re-renders even old notifications correctly,
        // rather than baking in whatever language was active when the notification was created.
        string language = _currentUserService.Language;
        List<NotificationDto> items = entities
            .Select(n => new NotificationDto(
                n.Id, n.Type.ToString(),
                MessageTranslator.Translate(n.Title, language), MessageTranslator.Translate(n.Body, language),
                n.RelatedEntityType, n.RelatedEntityId, n.IsRead, n.ReadAt, n.CreatedAt))
            .ToList();

        return new PagedResult<NotificationDto>(items, totalCount, page, pageSize);
    }
}
