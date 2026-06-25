using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentBridge.Applications.Application.Commands.Apply;
using TalentBridge.Applications.Application.Commands.UpdateStatus;
using TalentBridge.Applications.Application.Commands.Withdraw;
using TalentBridge.Applications.Application.Queries.GetApplication;
using TalentBridge.Applications.Application.Queries.GetApplicationHistory;
using TalentBridge.Applications.Application.Queries.GetApplications;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ApplicationsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("my")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyApplications(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApplicationsQuery(_currentUser.UserId, null), ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetApplications(
        [FromQuery] Guid? candidateId,
        [FromQuery] Guid? jobId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApplicationsQuery(candidateId, jobId), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Apply([FromBody] ApplyCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ApplicationId }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApplicationByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateApplicationStatusCommand(id, request.NewStatus, request.RejectionReason), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/withdraw")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new WithdrawApplicationCommand(id, _currentUser.UserId), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    [Authorize]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApplicationHistoryQuery(id), ct);
        return Ok(result);
    }
}

public record UpdateStatusRequest(string NewStatus, string? RejectionReason = null);
