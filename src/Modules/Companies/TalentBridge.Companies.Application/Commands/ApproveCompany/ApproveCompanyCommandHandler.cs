using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Companies.Application.Interfaces;
using TalentBridge.Shared.Common;

namespace TalentBridge.Companies.Application.Commands.ApproveCompany;

public class ApproveCompanyCommandHandler(ICompanyDbContext dbContext)
    : IRequestHandler<ApproveCompanyCommand, Result>
{
    public async Task<Result> Handle(ApproveCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company is null)
            return Result.Failure($"Company {request.CompanyId} not found.");

        var result = company.Approve(request.AdminId);
        if (!result.IsSuccess) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
