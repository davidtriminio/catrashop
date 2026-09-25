using CommerceHub.BuildingBlocks.Results;

namespace CommerceHub.Modules.Payments.Domain;

public static class PaymentErrors
{
    public static readonly Error OrderNotFound =
        Error.NotFound("payments.order_not_found", "The order to pay for was not found.");

    public static readonly Error OrderNotPayable =
        Error.Conflict("payments.order_not_payable", "This order is not awaiting payment.");

    public static readonly Error NotFound =
        Error.NotFound("payments.not_found", "Payment not found.");

    public static readonly Error NotRefundable =
        Error.Conflict("payments.not_refundable", "Only a captured payment can be refunded.");

    public static Error InvalidTransition(PaymentStatus from, PaymentStatus to) =>
        Error.Conflict("payments.invalid_transition", $"A payment cannot move from {from} to {to}.");

    public static readonly Error InvalidSignature =
        Error.Unauthorized("payments.invalid_signature", "The webhook signature is invalid.");
}
