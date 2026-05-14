using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Entities;

namespace OAI.Infrastructure.Persistence.Configurations;

public sealed class InvoiceSourceFileConfiguration : IEntityTypeConfiguration<InvoiceSourceFile>
{
    public void Configure(EntityTypeBuilder<InvoiceSourceFile> builder)
    {
        builder.ToTable("InvoiceSourceFiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId);

        builder.Property(x => x.UploadBatchFileId);

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StoredFilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.PreviewFilePath)
            .HasMaxLength(500);

        builder.Property(x => x.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.FileSizeBytes)
            .IsRequired();

        builder.Property(x => x.PageNumber);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.InvoiceId);

        builder.HasIndex(x => x.UploadBatchFileId);

        builder.HasIndex(x => new
        {
            x.InvoiceId,
            x.PageNumber
        });

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.SourceFiles)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.UploadBatchFile)
            .WithMany()
            .HasForeignKey(x => x.UploadBatchFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
