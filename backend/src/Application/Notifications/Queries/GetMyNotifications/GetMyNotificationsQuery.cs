using MediatR;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Notifications;

namespace ProcureLive.Application.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(bool? UnreadOnly, int Page, int PageSize) : IRequest<PagedResult<NotificationDto>>;
