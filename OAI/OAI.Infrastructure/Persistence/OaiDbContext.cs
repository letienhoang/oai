using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OAI.Domain.Audit;
using OAI.Domain.Entities;
using OAI.Infrastructure.Identity;

namespace OAI.Infrastructure.Persistence;

public sealed class OaiDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public OaiDbContext(DbContextOptions<OaiDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InvoiceExtractionResult> InvoiceExtractionResults => Set<InvoiceExtractionResult>();
    public DbSet<InvoiceSourceFile> InvoiceSourceFiles => Set<InvoiceSourceFile>();
    public DbSet<UploadBatch> UploadBatches => Set<UploadBatch>();
    public DbSet<ValidationIssue> ValidationIssues => Set<ValidationIssue>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OaiDbContext).Assembly);
    }
}
