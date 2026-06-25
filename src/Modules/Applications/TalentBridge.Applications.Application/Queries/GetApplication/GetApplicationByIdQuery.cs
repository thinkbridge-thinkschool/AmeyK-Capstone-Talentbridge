using MediatR;
using TalentBridge.Applications.Application.DTOs;

namespace TalentBridge.Applications.Application.Queries.GetApplication;

public record GetApplicationByIdQuery(Guid ApplicationId) : IRequest<ApplicationDetailDto?>;
