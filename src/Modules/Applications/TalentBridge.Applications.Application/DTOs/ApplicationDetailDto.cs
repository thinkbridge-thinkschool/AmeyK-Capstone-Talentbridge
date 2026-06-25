namespace TalentBridge.Applications.Application.DTOs;

public record ApplicationDetailDto(
    Guid Id,
    Guid CandidateId,
    Guid JobId,
    string Status,
    string CoverLetter,
    string ResumeUrl,
    DateTime SubmittedAtUtc,
    DateTime LastUpdatedAtUtc,
    string? RejectionReason = null);
