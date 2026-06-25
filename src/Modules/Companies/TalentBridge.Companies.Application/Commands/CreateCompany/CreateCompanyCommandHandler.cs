using MediatR;
using TalentBridge.Companies.Application.Interfaces;
using TalentBridge.Companies.Domain.Entities;
using TalentBridge.Shared.Common;

namespace TalentBridge.Companies.Application.Commands.CreateCompany;

public class CreateCompanyCommandHandler(ICompanyDbContext dbContext)
    : IRequestHandler<CreateCompanyCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var result = Company.Create(request.Name, request.Description, request.OwnerId);
        if (!result.IsSuccess) return Result<Guid>.Failure(result.Error!);

        var company = result.Value!;
        if (!string.IsNullOrWhiteSpace(request.Website))
            company.UpdateProfile(company.Name, company.Description, request.Website);

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(company.Id);
    }
}
