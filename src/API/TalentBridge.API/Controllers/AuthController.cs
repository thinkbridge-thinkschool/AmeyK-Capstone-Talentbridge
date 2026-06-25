using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TalentBridge.Identity.Application.Commands.Login;
using TalentBridge.Identity.Application.Commands.RefreshToken;
using TalentBridge.Identity.Application.Commands.Register;
using TalentBridge.Identity.Application.Commands.UpdateProfile;
using TalentBridge.Identity.Application.Queries.GetCurrentUser;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/identity")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (result.IsFailure)
            return Unauthorized(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (result.IsFailure)
            return Unauthorized(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(_currentUser.UserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProfileCommand(
                _currentUser.UserId,
                request.FullName,
                request.Phone,
                request.Title,
                request.Bio,
                request.Skills,
                request.ResumeUrl,
                request.LinkedInUrl,
                request.GitHubUrl),
            ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        var updated = await _mediator.Send(new GetCurrentUserQuery(_currentUser.UserId), ct);
        return Ok(updated);
    }
}

public record UpdateProfileRequest(
    string? FullName,
    string? Phone,
    string? Title,
    string? Bio,
    string? Skills,
    string? ResumeUrl,
    string? LinkedInUrl,
    string? GitHubUrl);
