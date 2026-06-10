using System.Security.Cryptography;
using System.Text;
using CommerceHub.Modules.Payments.Application.Abstractions;
using CommerceHub.Modules.Payments.Application.Configuration;
using Microsoft.Extensions.Options;

namespace CommerceHub.Modules.Payments.Infrastructure;

/// <summary>
/// Verifies a webhook by recomputing an HMAC-SHA256 of the raw body with the shared secret and comparing it to
/// the signature header in constant time. If no secret is configured, verification fails closed (rejects) rather
/// than open — a webhook must never be trusted without a configured secret.
/// </summary>
public sealed class HmacWebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private readonly PaymentsOptions _options;

    public HmacWebhookSignatureVerifier(IOptions<PaymentsOptions> options) => _options = options.Value;

    public bool Verify(string payload, string? signatureHeader)
    {
        if (string.IsNullOrEmpty(_options.WebhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var expected = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.WebhookSecret), Encoding.UTF8.GetBytes(payload)));

        var provided = signatureHeader.Trim();
        // Constant-time comparison to avoid timing leaks.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(provided.ToUpperInvariant()));
    }
}
