using Core.Entities;
using Core.Interfaces.Services;
using Infrastructure.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Infrastructure.Data.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor, IScopedService
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditInterceptor> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(
        IAuditService auditService,
        ILogger<AuditInterceptor> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditService = auditService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await AuditChangesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task AuditChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var correlationId = GetCorrelationId();
        var timestamp = DateTime.UtcNow;

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.Entity.AuditMode == AuditMode.None)
                continue;

            var auditEntry = CreateAuditEntry(entry, userId, correlationId, timestamp);
            if (auditEntry != null)
            {
                auditEntries.Add(auditEntry);
            }
        }

        if (auditEntries.Any())
        {
            try
            {
                await _auditService.LogAuditEntriesAsync(auditEntries, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit entries to MongoDB. Continuing with business operation.");
            }
        }
    }

    private AuditEntry? CreateAuditEntry(EntityEntry<AuditableEntity> entry, string? userId, string? correlationId, DateTime timestamp)
    {
        var entityType = entry.Entity.GetType();
        var tableName = entry.Context.Model.FindEntityType(entityType)?.GetTableName() ?? entityType.Name;

        var action = entry.State switch
        {
            EntityState.Added => "Create",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => null
        };

        if (action == null) return null;

        // Check audit mode
        var auditMode = entry.Entity.AuditMode;
        var shouldAudit = auditMode switch
        {
            AuditMode.CreateOnly => action == "Create",
            AuditMode.UpdateOnly => action == "Update",
            AuditMode.DeleteOnly => action == "Delete",
            AuditMode.All => true,
            _ => false
        };

        if (!shouldAudit) return null;

        var auditEntry = new AuditEntry
        {
            Table = tableName,
            EntityId = entry.Entity.Id.ToString(),
            Action = action,
            UserId = userId,
            Timestamp = timestamp,
            CorrelationId = correlationId
        };

        // Capture changes
        var changes = new List<AuditChange>();

        if (action == "Create")
        {
            auditEntry.After = GetEntityValues(entry.CurrentValues);
        }
        else if (action == "Update")
        {
            auditEntry.Before = GetEntityValues(entry.OriginalValues);
            auditEntry.After = GetEntityValues(entry.CurrentValues);

            foreach (var property in entry.Properties)
            {
                if (property.IsModified)
                {
                    changes.Add(new AuditChange
                    {
                        Field = property.Metadata.Name,
                        OldValue = property.OriginalValue?.ToString(),
                        NewValue = property.CurrentValue?.ToString()
                    });
                }
            }
        }
        else if (action == "Delete")
        {
            auditEntry.Before = GetEntityValues(entry.OriginalValues);
        }

        auditEntry.Changes = changes;
        return auditEntry;
    }

    private Dictionary<string, object?> GetEntityValues(PropertyValues values)
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in values.Properties)
        {
            result[property.Name] = values[property];
        }
        return result;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}