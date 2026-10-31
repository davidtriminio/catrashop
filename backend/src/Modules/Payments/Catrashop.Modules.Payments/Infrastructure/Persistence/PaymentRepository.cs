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

    public Task<Payment?> GetLatestForOrderAsync(Guid orderId, CancellationToken ct = default) =>
        _context.Payments.Where(p => p.OrderId == orderId).OrderByDescending(p => p.CreatedAtUtc).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Payment>> ListAsync(PaymentStatus? status, string? method, int take, CancellationToken ct = default)
    {
        var query = _context.Payments.AsNoTracking().AsQueryable();
        if (status is { } s) query = query.Where(p => p.Status == s);
        if (!string.IsNullOrWhiteSpace(method)) query = query.Where(p => p.Method == method);
        // Oldest first: the queue an operator works through.
        return await query.OrderBy(p => p.CreatedAtUtc).Take(take).ToListAsync(ct);
    }

    public Task<bool> WebhookEventExistsAsync(string eventId, CancellationToken ct = default) =>
        _context.WebhookEvents.AnyAsync(e => e.EventId == eventId, ct);

    public void AddWebhookEvent(ProcessedWebhookEvent webhookEvent) => _context.WebhookEvents.Add(webhookEvent);
}
