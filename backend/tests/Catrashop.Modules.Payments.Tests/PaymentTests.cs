using System.Security.Cryptography;
using System.Text;
using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Application.Configuration;
using Catrashop.Modules.Payments.Domain;
using Catrashop.Modules.Payments.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catrashop.Modules.Payments.Tests;

public class PaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static Payment NewPayment(decimal amount = 50m) =>
        Payment.Start(Guid.NewGuid(), Guid.NewGuid(), "key-1", amount, "USD", "fake", Now);

    // --- Payment state machine ------------------------------------------------------------------

    [Fact]
    public void Capture_from_pending_succeeds_and_records_reference()
    {
        var p = NewPayment();

        Assert.True(p.MarkCaptured("fake_ref", Now).IsSuccess);
        Assert.Equal(PaymentStatus.Captured, p.Status);
        Assert.Equal("fake_ref", p.ProviderReference);
        Assert.True(p.IsCaptured);
    }

    [Fact]
    public void Captured_can_be_refunded_but_not_failed()
    {
        var p = NewPayment();
        p.MarkCaptured("r", Now);

        Assert.True(p.MarkFailed("x", Now).IsFailure); // captured -> failed not allowed
        Assert.True(p.MarkRefunded(Now).IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, p.Status);
    }

    [Fact]
    public void Pending_cannot_jump_straight_to_refunded()
    {
        var p = NewPayment();

        var result = p.MarkRefunded(Now);

        Assert.True(result.IsFailure);
        Assert.Equal("payments.invalid_transition", result.Error.Code);
    }

    [Fact]
    public void Re_capturing_is_an_idempotent_no_op()
    {
        var p = NewPayment();
        p.MarkCaptured("r", Now);

        Assert.True(p.MarkCaptured("r", Now).IsSuccess); // same status → success, no throw
        Assert.Equal(PaymentStatus.Captured, p.Status);
    }

    [Fact]
    public void Failed_is_terminal()
    {
        var p = NewPayment();
        Assert.True(p.MarkFailed("declined", Now).IsSuccess);
        Assert.True(p.MarkCaptured("r", Now).IsFailure);
    }

    // --- Fake provider --------------------------------------------------------------------------

    [Fact]
    public async Task Fake_provider_captures_a_positive_amount()
    {
        var provider = new FakePaymentProvider();

        var result = await provider.AuthorizeAndCaptureAsync(new PaymentRequest(Guid.NewGuid(), "k", 10m, "USD"));

        Assert.True(result.Success);
        Assert.StartsWith("fake_", result.ProviderReference);
    }

    [Fact]
    public async Task Fake_provider_declines_a_non_positive_amount()
    {
        var provider = new FakePaymentProvider();

        var result = await provider.AuthorizeAndCaptureAsync(new PaymentRequest(Guid.NewGuid(), "k", 0m, "USD"));

        Assert.False(result.Success);
    }

    // --- Webhook signature ----------------------------------------------------------------------

    private static HmacWebhookSignatureVerifier Verifier(string secret) =>
        new(Options.Create(new PaymentsOptions { WebhookSecret = secret }));

    private static string Sign(string secret, string body) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));

    [Fact]
    public void Valid_signature_is_accepted()
    {
        const string secret = "s3cr3t";
        const string body = "{\"id\":\"evt_1\"}";

        Assert.True(Verifier(secret).Verify(body, Sign(secret, body)));
    }

    [Fact]
    public void Tampered_body_is_rejected()
    {
        const string secret = "s3cr3t";
        var sig = Sign(secret, "{\"id\":\"evt_1\"}");

        Assert.False(Verifier(secret).Verify("{\"id\":\"evt_TAMPERED\"}", sig));
    }

    [Fact]
    public void Missing_secret_fails_closed()
    {
        Assert.False(Verifier("").Verify("{}", "anything"));
    }
}
