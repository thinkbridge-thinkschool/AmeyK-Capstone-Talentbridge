using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Companies.Application.DTOs;
using TalentBridge.Companies.Application.Interfaces;

namespace TalentBridge.Companies.Application.Queries.GetAllCompanies;

public class GetAllCompaniesQueryHandler(ICompanyDbContext dbContext)
    : IRequestHandler<GetAllCompaniesQuery, List<CompanyDto>>
{
    public async Task<List<CompanyDto>> Handle(GetAllCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Companies
            .OrderBy(c => c.IsApproved)
            .ThenByDescending(c => c.CreatedAtUtc)
            .Select(c => new CompanyDto(c.Id, c.Name, c.Description, c.Website, c.IsApproved, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
