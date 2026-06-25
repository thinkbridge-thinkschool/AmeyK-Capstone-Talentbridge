using MediatR;

namespace TalentBridge.Jobs.Application.Commands.UpdateJob;

public record UpdateJobCommand(
    Guid JobId,
    Guid RequestedByHRId,
    string Title,
    string Description,
    string Location,
    decimal SalaryMin,
    decimal SalaryMax) : IRequest<Unit>;
