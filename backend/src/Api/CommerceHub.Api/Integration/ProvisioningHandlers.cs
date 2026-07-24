using CommerceHub.BuildingBlocks.Messaging;
using CommerceHub.Modules.Customers.Contracts;
using CommerceHub.Modules.Identity.Contracts;
using CommerceHub.Modules.Notifications.Contracts;

namespace CommerceHub.Api.Integration;

/// <summary>
/// On registration, provisions the customer profile (idempotent get-or-create) and sends a welcome notification.
/// This replaces the synchronous provisioning the endpoints did before: registration now flows Identity →
/// (event) → Customers, keeping the modules decoupled.
/// </summary>
public sealed class CustomerRegisteredHandler : IIntegrationEventHandler<CustomerRegisteredIntegrationEvent>
{
    private readonly ICustomerProvisioning _customers;
    private readonly INotificationDispatcher _notifications;

    public CustomerRegisteredHandler(ICustomerProvisioning customers, INotificationDispatcher notifications)
    {
        _customers = customers;
        _notifications = notifications;
    }

    public async Task HandleAsync(CustomerRegisteredIntegrationEvent e, CancellationToken ct = default)
    {
        await _customers.EnsureCustomerAsync(e.UserId, e.Email, e.FirstName, e.LastName, ct);
        await _notifications.DispatchAsync(new NotificationRequest(
            e.UserId, NotificationType.Welcome, e.EventId.ToString(),
            new Dictionary<string, string> { ["name"] = e.FirstName }, e.Email, $"{e.FirstName} {e.LastName}"), ct);
    }
}
