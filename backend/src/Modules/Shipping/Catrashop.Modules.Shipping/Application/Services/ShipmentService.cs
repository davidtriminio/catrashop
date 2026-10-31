using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Shipping.Application.Abstractions;
using Catrashop.Modules.Shipping.Application.Contracts;
using Catrashop.Modules.Shipping.Contracts;
using Catrashop.Modules.Shipping.Domain;

namespace Catrashop.Modules.Shipping.Application.Services;

/// <summary>
/// Creates and progresses shipments, keeping the order in step via <see cref="IOrderFulfillment"/>: creating a
/// shipment moves a paid order into Processing; shipping/delivering drives the order to Shipped/Delivered. One
/// shipment per order (enforced by a unique index and an existence check).
/// </summary>
public sealed class ShipmentService
{
    private const string DefaultCarrier = "Catrashop Logistics";

    private readonly IShipmentRepository _shipments;
    private readonly IShippingMethodRepository _methods;
    private readonly IOrderFulfillment _orders;
    private readonly IIntegrationEventPublisher _events;
    private readonly IShippingUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public ShipmentService(
        IShipmentRepository shipments,
        IShippingMethodRepository methods,
        IOrderFulfillment orders,
        IIntegrationEventPublisher events,
        IShippingUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _shipments = shipments;
        _methods = methods;
        _orders = orders;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ShipmentResponse>> CreateAsync(CreateShipmentRequest request, CancellationToken ct = default)
    {
        var method = await _methods.GetByCodeAsync(request.MethodCode.Trim().ToLowerInvariant(), activeOnly: true, ct);
        if (method is null)
            return ShippingErrors.MethodNotFound(request.MethodCode);

        if (await _shipments.ExistsForOrderAsync(request.OrderId, ct))
            return ShippingErrors.ShipmentAlreadyExists;

        var order = await _orders.GetFulfillmentAsync(request.OrderId, ct);
        if (order is null)
            return ShippingErrors.OrderNotFound;
        if (order.Status is not ("Paid" or "Confirmed" or "Processing"))
            return ShippingErrors.OrderNotShippable;

        // A paid or confirmed order enters processing when its shipment is created.
        if (order.Status is "Paid" or "Confirmed")
        {
            var processing = await _orders.StartProcessingAsync(request.OrderId, ct);
            if (processing.IsFailure)
                return processing.Error;
        }

        var now = _clock.GetUtcNow();
        var carrier = string.IsNullOrWhiteSpace(request.Carrier) ? DefaultCarrier : request.Carrier.Trim();
        var destination = new Destination(order.RecipientName, order.Line1, order.Line2, order.City, order.Region, order.PostalCode, order.Country);

        var shipment = Shipment.Create(request.OrderId, carrier, GenerateTrackingNumber(), method.Code,
            method.Cost, method.Currency, destination, now);
        _shipments.Add(shipment);
        _events.Enqueue(new ShipmentCreatedIntegrationEvent(shipment.Id, shipment.OrderId, shipment.Carrier, shipment.TrackingNumber), shipment.OrderId);
        await _unitOfWork.SaveChangesAsync(ct);

        return ShippingMappers.ToResponse(shipment);
    }

    public async Task<Result<ShipmentResponse>> MarkShippedAsync(Guid shipmentId, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct);
        if (shipment is null)
            return ShippingErrors.ShipmentNotFound;

        var t = shipment.MarkShipped(_clock.GetUtcNow());
        if (t.IsFailure)
            return t.Error;

        // Advance the order first (its own unit of work); only persist the shipment if that succeeds.
        var order = await _orders.MarkShippedAsync(shipment.OrderId, ct);
        if (order.IsFailure)
            return order.Error;

        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(shipment);
    }

    public async Task<Result<ShipmentResponse>> MarkDeliveredAsync(Guid shipmentId, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct);
        if (shipment is null)
            return ShippingErrors.ShipmentNotFound;

        var t = shipment.MarkDelivered(_clock.GetUtcNow());
        if (t.IsFailure)
            return t.Error;

        var order = await _orders.MarkDeliveredAsync(shipment.OrderId, ct);
        if (order.IsFailure)
            return order.Error;

        _events.Enqueue(new ShipmentDeliveredIntegrationEvent(shipment.Id, shipment.OrderId, shipment.TrackingNumber), shipment.OrderId);
        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(shipment);
    }

    public async Task<Result<ShipmentResponse>> CancelAsync(Guid shipmentId, string? reason, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct);
        if (shipment is null)
            return ShippingErrors.ShipmentNotFound;

        var t = shipment.Cancel(reason, _clock.GetUtcNow());
        if (t.IsFailure)
            return t.Error;

        await _unitOfWork.SaveChangesAsync(ct);
        return ShippingMappers.ToResponse(shipment);
    }

    public async Task<Result<ShipmentResponse>> GetAsync(Guid shipmentId, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct);
        return shipment is null ? ShippingErrors.ShipmentNotFound : ShippingMappers.ToResponse(shipment);
    }

    public async Task<Result<ShipmentResponse>> GetByOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByOrderIdAsync(orderId, ct);
        return shipment is null ? ShippingErrors.ShipmentNotFound : ShippingMappers.ToResponse(shipment);
    }

    /// <summary>The shipment for one of the caller's own orders (ownership verified via the order).</summary>
    public async Task<Result<ShipmentResponse>> GetForCustomerAsync(Guid customerId, Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetFulfillmentAsync(orderId, ct);
        if (order is null || order.CustomerId != customerId)
            return ShippingErrors.ShipmentNotFound; // don't reveal others' orders

        var shipment = await _shipments.GetByOrderIdAsync(orderId, ct);
        return shipment is null ? ShippingErrors.ShipmentNotFound : ShippingMappers.ToResponse(shipment);
    }

    public async Task<Result<TrackingResponse>> TrackAsync(string trackingNumber, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByTrackingNumberAsync(trackingNumber.Trim().ToUpperInvariant(), ct);
        return shipment is null ? ShippingErrors.ShipmentNotFound : ShippingMappers.ToTracking(shipment);
    }

    private static string GenerateTrackingNumber() => $"CH{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
}
