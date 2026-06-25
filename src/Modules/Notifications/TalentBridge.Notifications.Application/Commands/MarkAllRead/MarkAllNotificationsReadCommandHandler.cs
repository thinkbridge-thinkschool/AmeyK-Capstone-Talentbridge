using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Notifications.Application.Interfaces;

namespace TalentBridge.Notifications.Application.Commands.MarkAllRead;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Unit>
{
    private readonly INotificationsDbContext _dbContext;

    public MarkAllNotificationsReadCommandHandler(INotificationsDbContext dbContext) => _dbContext = dbContext;

    public async Task<Unit> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        await _dbContext.Notifications
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

        return Unit.Value;
    }
}
