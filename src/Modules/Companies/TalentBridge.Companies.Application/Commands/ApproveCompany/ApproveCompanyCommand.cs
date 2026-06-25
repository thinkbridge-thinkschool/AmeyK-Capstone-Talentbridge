using MediatR;
using TalentBridge.Shared.Common;

namespace TalentBridge.Companies.Application.Commands.ApproveCompany;

public record ApproveCompanyCommand(Guid CompanyId, Guid AdminId) : IRequest<Result>;
