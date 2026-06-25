using MediatR;

namespace TalentBridge.Notifications.Application.Commands.MarkRead;

public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<Unit>;
