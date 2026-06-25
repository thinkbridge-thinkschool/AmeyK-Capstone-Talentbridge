using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Notifications.Application.Interfaces;

namespace TalentBridge.Notifications.Application.Queries.GetNotifications;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly INotificationsDbContext _dbContext;

    public GetNotificationsQueryHandler(INotificationsDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == request.UserId)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAtUtc)
            .Select(n => new NotificationDto(n.Id, n.Message, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
