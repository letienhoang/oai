using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OAI.Infrastructure.Audit;

public sealed class AuditTrailInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AuditTrailInterceptor> _logger;

    public AuditTrailInterceptor(ILogger<AuditTrailInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context;
        if (context is null)
            return base.SavingChanges(eventData, result);

        var entries = BuildAuditEntries(context);
        if (entries.Count > 0)
        {
            context.Set<AuditLogEntry>().AddRange(entries);
            _logger.LogInformation("Audit entries created: {Count}", entries.Count);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<InterceptionResult<int>>(
            SavingChanges(eventData, result));
    }

    private static List<AuditLogEntry> BuildAuditEntries(DbContext context)
    {
        var auditEntries = new List<AuditLogEntry>();

        var trackedEntries = context.ChangeTracker.Entries()
            .Where(x => x.Entity is not AuditLogEntry)
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in trackedEntries)
        {
            var entityName = entry.Metadata.ClrType.Name;
            var entityId = GetEntityId(entry);
            var oldValues = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                ? GetValues(entry.OriginalValues)
                : null;

            var newValues = entry.State == EntityState.Added || entry.State == EntityState.Modified
                ? GetValues(entry.CurrentValues)
                : null;

            var actionType = entry.State switch
            {
                EntityState.Added => AuditActionType.Created,
                EntityState.Modified => AuditActionType.Updated,
                EntityState.Deleted => AuditActionType.Deleted,
                _ => AuditActionType.Updated
            };

            auditEntries.Add(new AuditLogEntry
            {
                EntityName = entityName,
                EntityId = entityId,
                ActionType = actionType,
                OldValuesJson = oldValues,
                NewValuesJson = newValues,
                OccurredAt = DateTimeOffset.UtcNow,
                Source = "EF Core SaveChanges"
            });
        }

        return auditEntries;
    }

    private static string? GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(x => x.Metadata.Name == "Id");
        return idProperty?.CurrentValue?.ToString();
    }

    private static string GetValues(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues values)
    {
        var dictionary = values.Properties.ToDictionary(
            p => p.Name,
            p => values[p]?.ToString());

        return JsonSerializer.Serialize(dictionary);
    }
}