using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Infrastructure.Services.Audit;

public class MongoAuditService : IAuditService
{
    private readonly IMongoCollection<AuditEntry> _auditCollection;
    private readonly ILogger<MongoAuditService> _logger;

    public MongoAuditService(IConfiguration configuration, ILogger<MongoAuditService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var databaseName = configuration["Mongo:Database"] ?? "AppDatabase";
        var collectionName = configuration["Audit:MongoCollection"] ?? "audit_logs";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _auditCollection = database.GetCollection<AuditEntry>(collectionName);

        // Create indexes
        CreateIndexes();
    }

    public async Task LogAuditEntriesAsync(IEnumerable<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entries.Any())
            {
                await _auditCollection.InsertManyAsync(entries, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert audit entries into MongoDB");
            throw;
        }
    }

    public async Task<IEnumerable<AuditEntry>> GetAuditTrailAsync(string table, string entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AuditEntry>.Filter.And(
                Builders<AuditEntry>.Filter.Eq(x => x.Table, table),
                Builders<AuditEntry>.Filter.Eq(x => x.EntityId, entityId)
            );

            var sort = Builders<AuditEntry>.Sort.Descending(x => x.Timestamp);

            return await _auditCollection
                .Find(filter)
                .Sort(sort)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit trail from MongoDB for {Table}:{EntityId}", table, entityId);
            throw;
        }
    }

    private void CreateIndexes()
    {
        try
        {
            var indexKeys = Builders<AuditEntry>.IndexKeys
                .Ascending(x => x.Table)
                .Ascending(x => x.EntityId)
                .Descending(x => x.Timestamp);

            var indexModel = new CreateIndexModel<AuditEntry>(indexKeys);
            _auditCollection.Indexes.CreateOne(indexModel);

            // Optional TTL index for automatic cleanup (uncomment if needed)
            // var ttlIndexKeys = Builders<AuditEntry>.IndexKeys.Ascending(x => x.Timestamp);
            // var ttlIndexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(365) };
            // var ttlIndexModel = new CreateIndexModel<AuditEntry>(ttlIndexKeys, ttlIndexOptions);
            // _auditCollection.Indexes.CreateOne(ttlIndexModel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create audit collection indexes");
        }
    }
}