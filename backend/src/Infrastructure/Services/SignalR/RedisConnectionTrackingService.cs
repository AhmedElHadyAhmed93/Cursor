using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services.SignalR;

public class RedisConnectionTrackingService : IConnectionTrackingService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisConnectionTrackingService> _logger;
    private const string KeyPrefix = "signalr:user:";

    public RedisConnectionTrackingService(IConfiguration configuration, ILogger<RedisConnectionTrackingService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        var redis = ConnectionMultiplexer.Connect(connectionString);
        _database = redis.GetDatabase();
    }

    public async Task AddConnectionAsync(string userId, string connectionId)
    {
        try
        {
            var key = KeyPrefix + userId;
            await _database.SetAddAsync(key, connectionId);
            await _database.KeyExpireAsync(key, TimeSpan.FromHours(24)); // Auto-cleanup after 24 hours
            
            _logger.LogDebug("Added connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
    }

    public async Task RemoveConnectionAsync(string userId, string connectionId)
    {
        try
        {
            var key = KeyPrefix + userId;
            await _database.SetRemoveAsync(key, connectionId);
            
            // Remove key if no connections left
            var count = await _database.SetLengthAsync(key);
            if (count == 0)
            {
                await _database.KeyDeleteAsync(key);
            }
            
            _logger.LogDebug("Removed connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
    }

    public async Task<IEnumerable<string>> GetConnectionsAsync(string userId)
    {
        try
        {
            var key = KeyPrefix + userId;
            var connections = await _database.SetMembersAsync(key);
            return connections.Select(c => c.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connections for user {UserId}", userId);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        try
        {
            var key = KeyPrefix + userId;
            var count = await _database.SetLengthAsync(key);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is online", userId);
            return false;
        }
    }
}