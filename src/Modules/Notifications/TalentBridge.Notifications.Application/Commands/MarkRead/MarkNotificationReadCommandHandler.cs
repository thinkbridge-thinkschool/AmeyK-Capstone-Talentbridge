using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Notifications.Application.Interfaces;

namespace TalentBridge.Notifications.Application.Commands.MarkRead;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly INotificationsDbContext _dbContext;

    public MarkNotificationReadCommandHandler(INotificationsDbContext dbContext) => _dbContext = dbContext;

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Notification {request.NotificationId} not found.");

        notification.IsRead = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
