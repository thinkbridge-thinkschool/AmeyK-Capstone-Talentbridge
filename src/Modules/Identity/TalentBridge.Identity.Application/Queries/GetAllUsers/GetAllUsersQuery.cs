using MediatR;
using TalentBridge.Identity.Application.Queries.GetCurrentUser;

namespace TalentBridge.Identity.Application.Queries.GetAllUsers;

public record GetAllUsersQuery : IRequest<List<CurrentUserDto>>;
