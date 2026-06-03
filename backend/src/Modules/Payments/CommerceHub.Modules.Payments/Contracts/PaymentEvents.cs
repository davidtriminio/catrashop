using CommerceHub.BuildingBlocks.Messaging;

namespace CommerceHub.Modules.Payments.Contracts;

public sealed record PaymentCapturedIntegrationEvent(Guid PaymentId, Guid OrderId, decimal Amount, string Currency) : IntegrationEvent;

public sealed record PaymentFailedIntegrationEvent(Guid PaymentId, Guid OrderId, string? Reason) : IntegrationEvent;

public sealed record RefundCompletedIntegrationEvent(Guid PaymentId, Guid OrderId, decimal Amount, string Currency) : IntegrationEvent;
