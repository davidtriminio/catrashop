namespace CommerceHub.BuildingBlocks.Security;

/// <summary>
/// The authenticated principal for the current request, resolved from the JWT by the API host. Modules
/// depend on this abstraction (not on HttpContext) to know who is acting — e.g. to scope queries to the
/// caller's own data.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
