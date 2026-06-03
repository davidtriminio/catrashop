using CommerceHub.Modules.Payments.Application.Abstractions;

namespace CommerceHub.Modules.Payments.Infrastructure;

/// <summary>
/// A deterministic simulated payment gateway (Section 7). It authorizes and captures in one step, succeeding for
/// any positive amount and declining a non-positive one, and always accepts refunds. It returns a synthetic
/// provider reference so the rest of the flow behaves exactly as it would with a real gateway — the seam where a
/// real provider (Stripe, etc.) drops in later.
/// </summary>
public sealed class FakePaymentProvider : IPaymentProvider
{
    public string Name => "fake";

    public Task<PaymentProviderResult> AuthorizeAndCaptureAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var result = request.Amount > 0
            ? PaymentProviderResult.Ok($"fake_{Guid.NewGuid():N}")
            : PaymentProviderResult.Fail("The amount must be greater than zero.");
        return Task.FromResult(result);
    }

    public Task<PaymentProviderResult> RefundAsync(string providerReference, decimal amount, CancellationToken ct = default) =>
        Task.FromResult(PaymentProviderResult.Ok($"fake_refund_{Guid.NewGuid():N}"));
}
