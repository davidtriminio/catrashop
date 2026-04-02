using CommerceHub.BuildingBlocks.Security;

namespace CommerceHub.Api.Security;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from the request's JWT claims. Inbound claim mapping is disabled
/// on the bearer handler, so claim types are read verbatim ("sub", "email").
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private System.Security.Claims.ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;

    public string? Email => Principal?.FindFirst("email")?.Value;
}
