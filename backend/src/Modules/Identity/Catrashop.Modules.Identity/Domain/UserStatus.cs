namespace Catrashop.Modules.Identity.Domain;

/// <summary>Lifecycle state of a user account. A locked account cannot authenticate.</summary>
public enum UserStatus
{
    Active = 0,
    Locked = 1
}
