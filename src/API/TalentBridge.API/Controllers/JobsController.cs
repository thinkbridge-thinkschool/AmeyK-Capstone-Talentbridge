using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentBridge.Jobs.Application.Commands.CloseJob;
using TalentBridge.Jobs.Application.Commands.PostJob;
using TalentBridge.Jobs.Application.Commands.PublishJob;
using TalentBridge.Jobs.Application.Queries.GetJobById;
using TalentBridge.Jobs.Application.Queries.SearchJobs;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator) => _mediator = mediator;

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
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchJobsQuery(keyword, location, type, page, size), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "CompanyHR")]
    public async Task<IActionResult> Publish(Guid id, [FromQuery] Guid companyId, CancellationToken ct)
    {
        await _mediator.Send(new PublishJobCommand(id, companyId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> Close(Guid id, [FromQuery] Guid companyId, CancellationToken ct)
    {
        await _mediator.Send(new CloseJobCommand(id, companyId), ct);
        return NoContent();
    }
}
