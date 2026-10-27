using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Notifications;

namespace ProcureLive.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public MarkNotificationReadCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        Notification? notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException(nameof(Notification), request.NotificationId);
        }

        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);
        bool ownsNotification = isInternalUser
            ? notification.CompanyId is null && (notification.UserId is null || notification.UserId == _currentUserService.UserId)
            : notification.CompanyId == _currentUserService.CompanyId;

        if (!ownsNotification)
        {
            throw new ForbiddenAccessException();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = _dateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
