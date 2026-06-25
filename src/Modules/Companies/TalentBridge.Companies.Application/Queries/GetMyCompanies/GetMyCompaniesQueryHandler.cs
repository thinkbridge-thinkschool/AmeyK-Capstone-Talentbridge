using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Companies.Application.DTOs;
using TalentBridge.Companies.Application.Interfaces;

namespace TalentBridge.Companies.Application.Queries.GetMyCompanies;

public class GetMyCompaniesQueryHandler(ICompanyDbContext dbContext)
    : IRequestHandler<GetMyCompaniesQuery, List<CompanyDto>>
{
    public async Task<List<CompanyDto>> Handle(GetMyCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Companies
            .AsNoTracking()
            .Where(c => c.OwnerId == request.OwnerId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CompanyDto(c.Id, c.Name, c.Description, c.Website, c.IsApproved, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
