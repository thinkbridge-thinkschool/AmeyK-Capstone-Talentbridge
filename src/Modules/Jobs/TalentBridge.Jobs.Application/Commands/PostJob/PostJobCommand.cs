using MediatR;

namespace TalentBridge.Jobs.Application.Commands.PostJob;

public record PostJobCommand(
    Guid CompanyId,
    string Title,
    string Description,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax,
    string JobType,
    List<string> RequiredSkills) : IRequest<PostJobResult>;

public record PostJobResult(Guid JobId, string Status);
