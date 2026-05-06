using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Audit;

namespace OAI.Infrastructure.Audit;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(100);

        builder.Property(x => x.UserId)
            .HasMaxLength(100);

        builder.Property(x => x.UserName)
            .HasMaxLength(200);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.Source)
            .HasMaxLength(200);

        builder.Property(x => x.OldValuesJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NewValuesJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.OccurredAt)
            .IsRequired();
    }
}