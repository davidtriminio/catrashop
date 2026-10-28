using CommerceHub.BuildingBlocks.Results;
using CommerceHub.Modules.Identity.Application.Abstractions;
using CommerceHub.Modules.Identity.Application.Configuration;
using CommerceHub.Modules.Identity.Application.Contracts;
using CommerceHub.Modules.Identity.Contracts;
using CommerceHub.Modules.Identity.Domain;
using Microsoft.Extensions.Options;

namespace CommerceHub.Modules.Identity.Application.Authentication;

/// <summary>
/// Handles account registration and the token lifecycle (login, refresh with rotation, logout).
/// Every path that touches credentials records a security-audit entry and returns generic failures
/// so the API never reveals whether an email exists.
/// </summary>
public sealed class AuthenticationService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ISecureTokenGenerator _secureTokens;
    private readonly ITokenHasher _tokenHasher;
    private readonly IIntegrationEventPublisher _events;
    private readonly ISecurityAuditLogger _audit;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly IdentitySecurityOptions _security;

    public AuthenticationService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwt,
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
        _jwt = jwt;
        _secureTokens = secureTokens;
        _tokenHasher = tokenHasher;
        _events = events;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _security = security.Value;
    }

    public async Task<Result<AuthTokensResponse>> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        if (await _users.EmailExistsAsync(email, ct))
            return IdentityErrors.EmailAlreadyInUse;

        var now = _clock.GetUtcNow();
        var user = User.Register(
            email,
            _passwordHasher.Hash(request.Password),
            request.FirstName.Trim(),
            request.LastName.Trim(),
            Role.Customer,
            now);

        _users.Add(user);

        var tokens = IssueSession(user, now);

        _events.Enqueue(new CustomerRegisteredIntegrationEvent(user.Id, user.Email, user.FirstName, user.LastName), user.Id);
        _audit.Record("user_registered", user.Id);

        await _unitOfWork.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<Result<AuthTokensResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var user = await _users.GetByEmailAsync(email, ct);

        // Verify a hash even when the user is missing, to keep timing uniform and avoid user enumeration.
        if (user is null)
        {
            _passwordHasher.Verify(request.Password, DummyHash);
            _audit.Record("login_failed", null, "unknown_email");
            await _unitOfWork.SaveChangesAsync(ct);
            return IdentityErrors.InvalidCredentials;
        }

        if (!user.CanAuthenticate)
        {
            _audit.Record("login_blocked", user.Id, "account_locked");
            await _unitOfWork.SaveChangesAsync(ct);
            return IdentityErrors.AccountLocked;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _audit.Record("login_failed", user.Id, "bad_password");
            await _unitOfWork.SaveChangesAsync(ct);
            return IdentityErrors.InvalidCredentials;
        }

        var now = _clock.GetUtcNow();
        var tokens = IssueSession(user, now);
        _audit.Record("login_succeeded", user.Id);

        await _unitOfWork.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<Result<AuthTokensResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var incomingHash = _tokenHasher.Hash(request.RefreshToken);
        var user = await _users.GetByRefreshTokenHashAsync(incomingHash, ct);
        if (user is null)
            return IdentityErrors.InvalidRefreshToken;

        var now = _clock.GetUtcNow();
        var current = user.FindRefreshToken(incomingHash);

        // Reuse detection: a token that exists but is no longer active means a revoked/rotated token was
        // replayed — treat as compromise and revoke every session for the user.
        if (current is not null && current.WasJustRotated(now, TimeSpan.FromSeconds(Math.Max(0, _security.RefreshReuseGraceSeconds))))
        {
            // Concurrent refresh with a token another request just rotated: hand out a sibling session token
            // instead of revoking the chain. The token already issued to the other request stays valid.
            var siblingRaw = _secureTokens.Generate();
            var siblingExpiry = now.AddDays(_security.RefreshTokenDays);
            user.IssueRefreshToken(_tokenHasher.Hash(siblingRaw), siblingExpiry, now);
            var siblingAccess = _jwt.Generate(user);
            _audit.Record("token_refreshed_concurrent", user.Id);
            await _unitOfWork.SaveChangesAsync(ct);
            return new AuthTokensResponse(siblingAccess.Value, "Bearer", siblingAccess.ExpiresAtUtc, siblingRaw, siblingExpiry);
        }

        if (current is null || !current.IsActive(now))
        {
            user.RevokeAllRefreshTokens(now);
            _audit.Record("refresh_reuse_detected", user.Id);
            await _unitOfWork.SaveChangesAsync(ct);
            return IdentityErrors.InvalidRefreshToken;
        }

        var newRefreshRaw = _secureTokens.Generate();
        var newRefreshHash = _tokenHasher.Hash(newRefreshRaw);
        var refreshExpiry = now.AddDays(_security.RefreshTokenDays);
        user.RotateRefreshToken(current, newRefreshHash, refreshExpiry, now);

        var access = _jwt.Generate(user);
        _audit.Record("token_refreshed", user.Id);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AuthTokensResponse(access.Value, "Bearer", access.ExpiresAtUtc, newRefreshRaw, refreshExpiry);
    }

    public async Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var incomingHash = _tokenHasher.Hash(request.RefreshToken);
        var user = await _users.GetByRefreshTokenHashAsync(incomingHash, ct);
        var token = user?.FindRefreshToken(incomingHash);
        if (user is not null && token is not null)
        {
            user.RevokeRefreshToken(token, _clock.GetUtcNow());
            _audit.Record("logout", user.Id);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // Idempotent: an unknown or already-revoked token still yields success.
        return Result.Success();
    }

    private AuthTokensResponse IssueSession(User user, DateTimeOffset now)
    {
        var refreshRaw = _secureTokens.Generate();
        var refreshHash = _tokenHasher.Hash(refreshRaw);
        var refreshExpiry = now.AddDays(_security.RefreshTokenDays);
        user.IssueRefreshToken(refreshHash, refreshExpiry, now);

        var access = _jwt.Generate(user);
        return new AuthTokensResponse(access.Value, "Bearer", access.ExpiresAtUtc, refreshRaw, refreshExpiry);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    // A well-formed bcrypt hash of a random value, used to equalize timing on the unknown-user path.
    private const string DummyHash = "$2a$11$ROa2p8Q0oZ2q1t9m3o9m1e5m8mVYyq7pQe1sQY3n8O2b0Cq7bF8aC";
}
