using Catrashop.BuildingBlocks.Results;

namespace Catrashop.Modules.Payments.Domain;

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

    public static readonly Error NotATransfer =
        Error.Conflict("payments.not_a_transfer", "This payment is not a bank transfer.");

    public static readonly Error NotCollectedLater =
        Error.Conflict("payments.not_collected_later", "Only a cash-on-delivery or pay-at-store payment is collected this way.");

    public static readonly Error NotAwaitingProof =
        Error.Conflict("payments.not_awaiting_proof", "This transfer is not waiting for a receipt.");

    public static readonly Error NotUnderReview =
        Error.Conflict("payments.not_under_review", "There is no submitted receipt to review on this payment.");

    public static readonly Error OrderNotDelivered =
        Error.Conflict("payments.order_not_delivered", "The money is collected once the order has been delivered or picked up.");

    public static readonly Error UnknownBankAccount =
        Error.Validation("payments.unknown_bank_account", "Choose one of the bank accounts listed for the transfer.");

    public static readonly Error ReceiptRequired =
        Error.Validation("payments.receipt_required", "Attach the receipt of the transfer.");

    public static readonly Error ReceiptTooLarge =
        Error.Validation("payments.receipt_too_large", "The receipt is larger than the allowed size.");

    public static readonly Error ReceiptUnsupported =
        Error.Validation("payments.receipt_unsupported", "The receipt must be a JPG or PNG image, or a PDF.");

    public static readonly Error ReferenceRequired =
        Error.Validation("payments.reference_required", "Enter the reference of the transfer.");

    public static readonly Error ReceiptNotFound =
        Error.NotFound("payments.receipt_not_found", "This payment has no receipt.");

    public static readonly Error InvalidSignature =
        Error.Unauthorized("payments.invalid_signature", "The webhook signature is invalid.");
}
