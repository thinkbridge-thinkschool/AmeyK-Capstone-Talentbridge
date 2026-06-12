using TalentBridge.Jobs.Domain.Enums;

namespace TalentBridge.Jobs.Application.DTOs;

public record JobDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    string Description,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    JobStatus Status,
    JobType Type,
    DateTime? ClosingDate,
    DateTime CreatedAt,
    List<string> RequiredSkills);
