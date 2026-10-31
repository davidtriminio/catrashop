using Catrashop.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="User"/> aggregate. Roles, refresh tokens and reset tokens are modeled as owned
/// collections so they are always loaded and saved with the user (true aggregate semantics) and live in
/// their own tables within the identity schema.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");

        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.EmailConfirmed);
        builder.Property(u => u.CreatedAtUtc);
        builder.Property(u => u.UpdatedAtUtc);

        // Ignore the computed projections (not stored).
        builder.Ignore(u => u.Roles);
        builder.Ignore(u => u.Permissions);

        builder.OwnsMany(u => u.RoleAssignments, role =>
        {
            role.ToTable("user_roles");
            role.WithOwner().HasForeignKey(r => r.UserId);
            role.HasKey(r => new { r.UserId, r.Role });
            role.Property(r => r.Role).HasConversion<string>().HasMaxLength(30);
        });

        builder.OwnsMany(u => u.RefreshTokens, token =>
        {
            token.ToTable("refresh_tokens");
            token.WithOwner().HasForeignKey(t => t.UserId);
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            token.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
            token.HasIndex(t => t.TokenHash).HasDatabaseName("ix_refresh_tokens_hash");
        });

        builder.OwnsMany(u => u.PasswordResetTokens, token =>
        {
            token.ToTable("password_reset_tokens");
            token.WithOwner().HasForeignKey(t => t.UserId);
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            token.HasIndex(t => t.TokenHash).HasDatabaseName("ix_password_reset_tokens_hash");
        });

        // Owned collections are backed by private fields; access them through those fields.
        builder.Metadata.FindNavigation(nameof(User.RoleAssignments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.PasswordResetTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable("security_audit_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.EventType).HasMaxLength(80).IsRequired();
        builder.Property(l => l.Detail).HasMaxLength(500);
        builder.HasIndex(l => l.OccurredAtUtc).HasDatabaseName("ix_security_audit_logs_occurred");
        builder.HasIndex(l => l.UserId).HasDatabaseName("ix_security_audit_logs_user");
    }
}
