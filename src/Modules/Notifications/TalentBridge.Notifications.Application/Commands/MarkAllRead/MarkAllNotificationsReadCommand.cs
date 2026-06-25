using MediatR;

namespace TalentBridge.Notifications.Application.Commands.MarkAllRead;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Unit>;
