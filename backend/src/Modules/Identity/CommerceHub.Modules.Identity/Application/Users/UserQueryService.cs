using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Identity.Application.Abstractions;
using CommerceHub.Modules.Identity.Application.Contracts;
using CommerceHub.Modules.Identity.Domain;

namespace CommerceHub.Modules.Identity.Application.Users;

/// <summary>Read-side queries over users (the authenticated user's own profile).</summary>
public sealed class UserQueryService
{
    private readonly IUserRepository _users;

    public UserQueryService(IUserRepository users) => _users = users;

    public async Task<Result<UserProfileResponse>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return IdentityErrors.UserNotFound;

        return ToProfile(user);
    }

    internal static UserProfileResponse ToProfile(User user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.EmailConfirmed,
        user.Roles.Select(r => r.ToString()).ToArray(),
        user.Permissions.ToArray());
}
