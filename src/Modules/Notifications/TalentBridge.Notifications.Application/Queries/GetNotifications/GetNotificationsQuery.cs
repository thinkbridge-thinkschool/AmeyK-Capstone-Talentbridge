using MediatR;

namespace TalentBridge.Notifications.Application.Queries.GetNotifications;

public record GetNotificationsQuery(Guid UserId) : IRequest<List<NotificationDto>>;

public record NotificationDto(Guid Id, string Message, bool IsRead, DateTime CreatedAtUtc);
