using Catrashop.BuildingBlocks.Messaging;

namespace Catrashop.Modules.Shipping.Contracts;

/// <summary>Published when a shipment is created for an order.</summary>
public sealed record ShipmentCreatedIntegrationEvent(Guid ShipmentId, Guid OrderId, string Carrier, string TrackingNumber) : IntegrationEvent;

/// <summary>Published when a shipment is delivered — drives the n8n "review request" automation (workflow 4).</summary>
public sealed record ShipmentDeliveredIntegrationEvent(Guid ShipmentId, Guid OrderId, string TrackingNumber) : IntegrationEvent;
