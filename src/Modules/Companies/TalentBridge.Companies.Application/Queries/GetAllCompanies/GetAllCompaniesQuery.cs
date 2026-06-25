using MediatR;
using TalentBridge.Companies.Application.DTOs;

namespace TalentBridge.Companies.Application.Queries.GetAllCompanies;

public record GetAllCompaniesQuery : IRequest<List<CompanyDto>>;
