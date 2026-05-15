using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Entities;

namespace OAI.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.SourceFileName)
            .HasMaxLength(255);

        builder.Property(x => x.SourceFilePath)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IssueDate)
            .IsRequired();

        builder.Property(x => x.DueDate);

        builder.Property(x => x.VendorId)
            .IsRequired();

        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(x => x.DeclaredSubtotal, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("DeclaredSubtotalAmount")
                .HasPrecision(18, 2);
            money.Property(x => x.Currency).HasColumnName("DeclaredSubtotalCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(x => x.DeclaredTaxAmount, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("DeclaredTaxAmount")
                .HasPrecision(18, 2);
            money.Property(x => x.Currency).HasColumnName("DeclaredTaxCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(x => x.DeclaredTotalAmount, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("DeclaredTotalAmount")
                .HasPrecision(18, 2);
            money.Property(x => x.Currency).HasColumnName("DeclaredTotalCurrency").HasMaxLength(10);
        });

        builder.HasMany(x => x.LineItems)
            .WithOne()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ValidationIssues)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ExtractionResults)
            .WithOne()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.SourceFiles)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
