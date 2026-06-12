using MediatR;

namespace TalentBridge.Applications.Application.Commands.Apply;

public record ApplyCommand(
    Guid JobId,
    Guid CandidateId,
    string CoverLetter,
    string ResumeUrl) : IRequest<ApplyResult>;

public record ApplyResult(Guid ApplicationId, string Status);
