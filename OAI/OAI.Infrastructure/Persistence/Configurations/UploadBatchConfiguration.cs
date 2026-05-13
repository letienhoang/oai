using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAI.Domain.Entities;

namespace OAI.Infrastructure.Persistence.Configurations;

public sealed class UploadBatchConfiguration : IEntityTypeConfiguration<UploadBatch>
{
    public void Configure(EntityTypeBuilder<UploadBatch> builder)
    {
        builder.ToTable("UploadBatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UploadedByUserId);

        builder.Property(x => x.UploadedByUserName)
            .HasMaxLength(256);

        builder.Property(x => x.TotalFiles)
            .IsRequired();

        builder.Property(x => x.ProcessedFiles)
            .IsRequired();

        builder.Property(x => x.FailedFiles)
            .IsRequired();

        builder.Property(x => x.OriginalZipFilePath)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.StartedAt);

        builder.Property(x => x.CompletedAt);

        builder.HasIndex(x => x.BatchCode)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.CreatedAt);
    }
}