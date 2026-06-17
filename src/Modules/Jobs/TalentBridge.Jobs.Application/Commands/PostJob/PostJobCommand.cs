using MediatR;

namespace TalentBridge.Jobs.Application.Commands.PostJob;

public record PostJobCommand(
    string Title,
    string Description,
    Guid CompanyId,
    Guid PostedByHRId,
    decimal SalaryMin,
    decimal SalaryMax,
    string Location) : IRequest<PostJobResult>;

public record PostJobResult(Guid JobId, string Status);
