namespace Catrashop.Modules.Payments.Domain;

/// <summary>Payment lifecycle states (Section 7). Transitions are guarded by <see cref="PaymentStateMachine"/>.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    Failed = 3,
    Refunded = 4
}

public static class PaymentStateMachine
{
    private static readonly IReadOnlyDictionary<PaymentStatus, IReadOnlySet<PaymentStatus>> Allowed =
        new Dictionary<PaymentStatus, IReadOnlySet<PaymentStatus>>
        {
            [PaymentStatus.Pending] = Set(PaymentStatus.Authorized, PaymentStatus.Captured, PaymentStatus.Failed),
            [PaymentStatus.Authorized] = Set(PaymentStatus.Captured, PaymentStatus.Failed),
            [PaymentStatus.Captured] = Set(PaymentStatus.Refunded),
            [PaymentStatus.Failed] = Set(),
            [PaymentStatus.Refunded] = Set()
        };

    public static bool CanTransition(PaymentStatus from, PaymentStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    private static IReadOnlySet<PaymentStatus> Set(params PaymentStatus[] s) => new HashSet<PaymentStatus>(s);
}
