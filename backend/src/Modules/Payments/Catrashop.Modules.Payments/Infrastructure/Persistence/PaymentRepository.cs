using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _context;
    public PaymentRepository(PaymentsDbContext context) => _context = context;

    public void Add(Payment payment) => _context.Payments.Add(payment);

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default) =>
        _context.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);

    public Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken ct = default) =>
        _context.Payments.FirstOrDefaultAsync(p => p.ProviderReference == providerReference, ct);

    public Task<bool> WebhookEventExistsAsync(string eventId, CancellationToken ct = default) =>
        _context.WebhookEvents.AnyAsync(e => e.EventId == eventId, ct);

    public void AddWebhookEvent(ProcessedWebhookEvent webhookEvent) => _context.WebhookEvents.Add(webhookEvent);
}
