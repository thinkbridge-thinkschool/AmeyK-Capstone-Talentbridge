using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;

namespace TalentBridge.Applications.Application.Commands.UploadResume;

public class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, UploadResumeResult>
{
    private readonly IResumeStorageService _storageService;
    private readonly ILogger<UploadResumeCommandHandler> _logger;

    public UploadResumeCommandHandler(IResumeStorageService storageService, ILogger<UploadResumeCommandHandler> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<UploadResumeResult> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        var url = await _storageService.UploadResumeAsync(
            request.CandidateId,
            request.File,
            request.FileName,
            request.ContentType,
            cancellationToken);

        _logger.LogInformation("[Applications] Resume uploaded for candidate {CandidateId}: {Url}", request.CandidateId, url);

        return new UploadResumeResult(url);
    }
}
