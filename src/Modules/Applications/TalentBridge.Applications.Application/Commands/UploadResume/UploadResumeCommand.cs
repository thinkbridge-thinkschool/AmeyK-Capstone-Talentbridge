using MediatR;

namespace TalentBridge.Applications.Application.Commands.UploadResume;

public record UploadResumeCommand(
    Guid CandidateId,
    Stream File,
    string FileName,
    string ContentType) : IRequest<UploadResumeResult>;

public record UploadResumeResult(string ResumeUrl);
