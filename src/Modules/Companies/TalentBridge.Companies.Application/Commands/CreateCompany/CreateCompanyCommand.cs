using MediatR;
using TalentBridge.Shared.Common;

namespace TalentBridge.Companies.Application.Commands.CreateCompany;

public record CreateCompanyCommand(string Name, string Description, string? Website, Guid OwnerId)
    : IRequest<Result<Guid>>;
