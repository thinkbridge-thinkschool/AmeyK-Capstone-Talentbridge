using MediatR;
using TalentBridge.Companies.Application.DTOs;

namespace TalentBridge.Companies.Application.Queries.GetMyCompanies;

public record GetMyCompaniesQuery(Guid OwnerId) : IRequest<List<CompanyDto>>;
