namespace TalentBridge.Jobs.Application.DTOs;

public record PagedResult<T>(List<T> Items, int TotalCount);
