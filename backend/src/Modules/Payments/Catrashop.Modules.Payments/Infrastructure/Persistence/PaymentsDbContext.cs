using Catrashop.Infrastructure.Persistence;
using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catrashop.Modules.Payments.Infrastructure.Persistence;

/// <summary>EF Core context for the Payments module. Owns the "payments" PostgreSQL schema.</summary>
public sealed class PaymentsDbContext : ModuleDbContext, IPaymentUnitOfWork
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    protected override string Schema => "payments";

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProcessedWebhookEvent> WebhookEvents => Set<ProcessedWebhookEvent>();
}
