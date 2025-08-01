using Core.Interfaces.Services;

namespace Infrastructure.Services.Audit;

public interface IAuditService : IScopedService
{
    Task LogAuditEntriesAsync(IEnumerable<AuditEntry> entries, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string table, string entityId, CancellationToken cancellationToken = default);
}

public class AuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Table { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, object?>? Before { get; set; }
    public Dictionary<string, object?>? After { get; set; }
    public List<AuditChange> Changes { get; set; } = new();
}

public class AuditChange
{
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}