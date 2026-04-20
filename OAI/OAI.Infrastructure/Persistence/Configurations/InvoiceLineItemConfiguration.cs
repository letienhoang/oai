using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Entities;

namespace OAI.Infrastructure.Persistence.Configurations;

public sealed class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LineNo)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TaxRate)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.OwnsOne(x => x.UnitPrice, money =>
        {
            money.Property(x => x.Amount).HasColumnName("UnitPriceAmount");
            money.Property(x => x.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(10);
        });

        builder.Ignore(x => x.NetAmount);
        builder.Ignore(x => x.TaxAmount);
        builder.Ignore(x => x.GrossAmount);
    }
}