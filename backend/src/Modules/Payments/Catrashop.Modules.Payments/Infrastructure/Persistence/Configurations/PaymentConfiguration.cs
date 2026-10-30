using Catrashop.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Modules.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.IdempotencyKey).HasMaxLength(200).IsRequired();
        // The idempotency key makes "pay this order" safe to retry — one payment per key.
        builder.HasIndex(p => p.IdempotencyKey).IsUnique().HasDatabaseName("ux_payments_idempotency_key");

        builder.HasIndex(p => p.OrderId).HasDatabaseName("ix_payments_order");
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Provider).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ProviderReference).HasMaxLength(100);
        builder.HasIndex(p => p.ProviderReference).HasDatabaseName("ix_payments_provider_reference");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
    }
}

public sealed class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.ToTable("processed_webhook_events");
        builder.HasKey(e => e.EventId); // provider event id is the natural key → dedupe
        builder.Property(e => e.EventId).HasMaxLength(200);
        builder.Property(e => e.Provider).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(80).IsRequired();
    }
}
