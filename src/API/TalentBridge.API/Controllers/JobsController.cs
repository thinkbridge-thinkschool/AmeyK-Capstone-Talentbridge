using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentBridge.Jobs.Application.Commands.CloseJob;
using TalentBridge.Jobs.Application.Commands.DeleteJob;
using TalentBridge.Jobs.Application.Commands.PostJob;
using TalentBridge.Jobs.Application.Commands.PublishJob;
using TalentBridge.Jobs.Application.Commands.UpdateJob;
using TalentBridge.Jobs.Application.Queries.GetJobById;
using TalentBridge.Jobs.Application.Queries.GetMyJobs;
using TalentBridge.Jobs.Application.Queries.SearchJobs;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public JobsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> PostJob([FromBody] PostJobCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.JobId }, result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetJobByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? location,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] decimal? salaryMin = null,
        [FromQuery] decimal? salaryMax = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchJobsQuery(keyword, location, page, size, salaryMin, salaryMax), ct);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> GetMyJobs(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyJobsQuery(_currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateJobCommand(id, _currentUser.UserId, request.Title, request.Description, request.Location, request.SalaryMin, request.SalaryMax), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteJobCommand(id, _currentUser.UserId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new PublishJobCommand(id, _currentUser.UserId), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/close")]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CloseJobCommand(id, _currentUser.UserId), ct);
        return NoContent();
    }
}

public record UpdateJobRequest(string Title, string Description, string Location, decimal SalaryMin, decimal SalaryMax);
