namespace TalentBridge.Companies.Application.DTOs;

public record CompanyDto(Guid Id, string Name, string Description, string? Website, bool IsApproved, DateTime CreatedAtUtc);
