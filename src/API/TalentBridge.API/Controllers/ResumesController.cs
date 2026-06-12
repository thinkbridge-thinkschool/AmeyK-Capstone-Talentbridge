using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TalentBridge.Applications.Application.Commands.UploadResume;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/resumes")]
public class ResumesController : ControllerBase
{
    private readonly IMediator _mediator;
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".doc", ".docx"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public ResumesController(IMediator mediator) => _mediator = mediator;

    [HttpPost("upload")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"File type '{extension}' is not allowed.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest($"File size exceeds the 5MB limit.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var candidateId))
            return Unauthorized();

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new UploadResumeCommand(candidateId, stream, file.FileName, file.ContentType),
            ct);

        return Ok(new { ResumeUrl = result.ResumeUrl });
    }
}
