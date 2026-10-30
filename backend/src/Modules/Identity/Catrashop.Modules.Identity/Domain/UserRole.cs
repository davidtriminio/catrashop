namespace Catrashop.Modules.Identity.Domain;

/// <summary>
/// A role assignment on a user. Modeled as a child of the <see cref="User"/> aggregate (table
/// <c>user_roles</c>) so role membership is queryable and each assignment is a row rather than an
/// opaque blob.
/// </summary>
public sealed class UserRole
{
    private UserRole() { } // EF

    internal UserRole(Guid userId, Role role)
    {
        UserId = userId;
        Role = role;
    }

    public Guid UserId { get; private set; }
    public Role Role { get; private set; }
}
