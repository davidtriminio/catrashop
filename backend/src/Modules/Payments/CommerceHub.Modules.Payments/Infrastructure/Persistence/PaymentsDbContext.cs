using CommerceHub.Infrastructure.Persistence;
using CommerceHub.Modules.Payments.Application.Abstractions;
using CommerceHub.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.Modules.Payments.Infrastructure.Persistence;

/// <summary>EF Core context for the Payments module. Owns the "payments" PostgreSQL schema.</summary>
public sealed class PaymentsDbContext : ModuleDbContext, IPaymentUnitOfWork
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    protected override string Schema => "payments";

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProcessedWebhookEvent> WebhookEvents => Set<ProcessedWebhookEvent>();
}
