using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentBridge.Companies.Application.Commands.CreateCompany;
using TalentBridge.Companies.Application.Queries.GetMyCompanies;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "CompanyHR,Admin")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateCompanyCommand(request.Name, request.Description, request.Website, currentUser.UserId), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetMyCompanies), new { }, new { id = result.Value });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyCompanies(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyCompaniesQuery(currentUser.UserId), ct);
        return Ok(result);
    }
}

public record CreateCompanyRequest(string Name, string Description, string? Website);
