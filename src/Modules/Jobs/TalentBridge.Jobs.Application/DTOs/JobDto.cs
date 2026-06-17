using TalentBridge.Jobs.Domain.Enums;

namespace TalentBridge.Jobs.Application.DTOs;

public record JobDto(
    Guid Id,
    Guid CompanyId,
    Guid PostedByHRId,
    string Title,
    string Description,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    JobStatus Status,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? ExpiresAtUtc);
