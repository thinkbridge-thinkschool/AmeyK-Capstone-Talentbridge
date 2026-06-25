using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentBridge.Companies.Application.Commands.ApproveCompany;
using TalentBridge.Companies.Application.Queries.GetAllCompanies;
using TalentBridge.Identity.Application.Commands.DeactivateUser;
using TalentBridge.Identity.Application.Queries.GetAllUsers;
using TalentBridge.Jobs.Application.Queries.GetAllJobsAdmin;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllUsersQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeactivateUserCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllJobs(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllJobsAdminQuery(), ct);
        return Ok(result);
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetAllCompanies(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllCompaniesQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("companies/{id:guid}/approve")]
    public async Task<IActionResult> ApproveCompany(Guid id, CancellationToken ct)
    {
        var adminId = currentUser.UserId;
        var result = await mediator.Send(new ApproveCompanyCommand(id, adminId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }
}
