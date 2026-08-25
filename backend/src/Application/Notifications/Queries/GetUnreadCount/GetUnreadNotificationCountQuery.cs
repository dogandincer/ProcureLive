using MediatR;

namespace ProcureLive.Application.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadNotificationCountQuery : IRequest<int>;
