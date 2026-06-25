using MediatR;

namespace TalentBridge.Applications.Application.Commands.Withdraw;

public record WithdrawApplicationCommand(Guid ApplicationId, Guid CandidateId) : IRequest<Unit>;
