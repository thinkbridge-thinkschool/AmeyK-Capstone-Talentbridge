using MediatR;

namespace TalentBridge.Applications.Application.Queries.GetApplications;

public record ApplicationSummaryDto(
    Guid Id,
    Guid CandidateId,
    Guid JobId,
    string Status,
    string CoverLetter,
    string ResumeUrl,
    DateTime SubmittedAtUtc,
    DateTime LastUpdatedAtUtc,
    decimal? MatchPercentage = null,
    Guid? ReviewedByHRId = null);

public record GetApplicationsQuery(Guid? CandidateId, Guid? JobId) : IRequest<List<ApplicationSummaryDto>>;
