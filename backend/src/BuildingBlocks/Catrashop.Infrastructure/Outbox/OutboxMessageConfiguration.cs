using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catrashop.Infrastructure.Outbox;

/// <summary>
/// EF mapping for <see cref="OutboxMessage"/>. Each module includes this in its own schema so the
/// outbox write participates in the module's transaction. The index on unprocessed rows keeps the
/// publisher's polling query cheap.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Content).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OccurredOnUtc).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(4000);

        // Publisher scans for not-yet-processed messages, oldest first.
        builder.HasIndex(m => new { m.ProcessedOnUtc, m.OccurredOnUtc })
               .HasDatabaseName("ix_outbox_messages_unprocessed");
    }
}
