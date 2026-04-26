namespace OAI.Infrastructure.Audit;

public interface IAuditTrailService
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}