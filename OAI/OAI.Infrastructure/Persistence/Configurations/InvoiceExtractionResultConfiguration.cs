using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Entities;

namespace OAI.Infrastructure.Persistence.Configurations;

public sealed class InvoiceExtractionResultConfiguration : IEntityTypeConfiguration<InvoiceExtractionResult>
{
    public void Configure(EntityTypeBuilder<InvoiceExtractionResult> builder)
    {
        builder.ToTable("InvoiceExtractionResults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EngineName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ConfidenceScore)
            .HasPrecision(5, 4);

        builder.Property(x => x.AttemptNo)
            .IsRequired();

        builder.Property(x => x.IsSuccessful)
            .IsRequired();

        builder.Property(x => x.RawText)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.StructuredJson)
            .HasColumnType("nvarchar(max)");
    }
}