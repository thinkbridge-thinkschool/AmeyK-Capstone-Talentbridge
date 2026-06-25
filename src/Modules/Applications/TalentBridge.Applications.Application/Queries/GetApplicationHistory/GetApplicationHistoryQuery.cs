using MediatR;

namespace TalentBridge.Applications.Application.Queries.GetApplicationHistory;

public record ApplicationHistoryDto(
    Guid Id,
    string FromStatus,
    string ToStatus,
    Guid? ChangedByUserId,
    string? Notes,
    DateTime ChangedAtUtc);

public record GetApplicationHistoryQuery(Guid ApplicationId) : IRequest<List<ApplicationHistoryDto>>;
