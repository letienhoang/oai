using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Entities;

namespace OAI.Infrastructure.Persistence.Configurations;

public sealed class ValidationIssueConfiguration : IEntityTypeConfiguration<ValidationIssue>
{
    public void Configure(EntityTypeBuilder<ValidationIssue> builder)
    {
        builder.ToTable("ValidationIssues");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RuleCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DetectedAt)
            .IsRequired();

        builder.Property(x => x.IsResolved)
            .IsRequired();
        
        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.ValidationIssues)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}