using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Identity.Application.Abstractions;
using Catrashop.Modules.Identity.Application.Configuration;
using Catrashop.Modules.Identity.Application.Contracts;
using Catrashop.Modules.Identity.Contracts;
using Catrashop.Modules.Identity.Domain;
using Microsoft.Extensions.Options;

namespace Catrashop.Modules.Identity.Application.Passwords;

/// <summary>
/// Password-reset request and completion. "Forgot" always reports success (never revealing whether an
/// email exists); the reset link is delivered out-of-band via an integration event. Completing a reset
/// revokes every active session for the account.
/// </summary>
public sealed class PasswordRecoveryService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecureTokenGenerator _secureTokens;
    private readonly ITokenHasher _tokenHasher;
    private readonly IIntegrationEventPublisher _events;
    private readonly ISecurityAuditLogger _audit;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly IdentitySecurityOptions _security;

    public PasswordRecoveryService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ISecureTokenGenerator secureTokens,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher events,
        ISecurityAuditLogger audit,
        IIdentityUnitOfWork unitOfWork,
        TimeProvider clock,
        IOptions<IdentitySecurityOptions> security)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _secureTokens = secureTokens;
        _tokenHasher = tokenHasher;
        _events = events;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _security = security.Value;
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is not null)
        {
            var now = _clock.GetUtcNow();
            var raw = _secureTokens.Generate();
            var expiry = now.AddHours(_security.PasswordResetTokenHours);
            user.IssuePasswordReset(_tokenHasher.Hash(raw), expiry, now);

            _events.Enqueue(new PasswordResetRequestedIntegrationEvent(user.Id, user.Email, user.FirstName, raw, expiry), user.Id);
            _audit.Record("password_reset_requested", user.Id);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // Always success — do not disclose account existence.
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var tokenHash = _tokenHasher.Hash(request.Token);
        var user = await _users.GetByResetTokenHashAsync(tokenHash, ct);
        if (user is null)
            return IdentityErrors.InvalidResetToken;

        var now = _clock.GetUtcNow();
        var resetToken = user.FindValidPasswordReset(tokenHash, now);
        if (resetToken is null)
            return IdentityErrors.InvalidResetToken;

        user.ResetPassword(resetToken, _passwordHasher.Hash(request.NewPassword), now);
        _audit.Record("password_reset_completed", user.Id);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
