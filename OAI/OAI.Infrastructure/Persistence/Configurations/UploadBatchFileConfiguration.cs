using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Entities;

namespace OAI.Infrastructure.Persistence.Configurations;

public sealed class UploadBatchFileConfiguration : IEntityTypeConfiguration<UploadBatchFile>
{
    public void Configure(EntityTypeBuilder<UploadBatchFile> builder)
    {
        builder.ToTable("UploadBatchFiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UploadBatchId)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StoredFilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.FileSizeBytes)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.InvoiceId);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.ProcessingStartedAt);

        builder.Property(x => x.ProcessingCompletedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.UploadBatchId);

        builder.HasIndex(x => x.InvoiceId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => new
        {
            x.UploadBatchId,
            x.OriginalFileName
        });

        builder.HasOne(x => x.UploadBatch)
            .WithMany(x => x.Files)
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}