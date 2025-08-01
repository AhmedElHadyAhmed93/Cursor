using Core.Interfaces.Services;

namespace Infrastructure.Services.SignalR;

public interface IConnectionTrackingService : IScopedService
{
    Task AddConnectionAsync(string userId, string connectionId);
    Task RemoveConnectionAsync(string userId, string connectionId);
    Task<IEnumerable<string>> GetConnectionsAsync(string userId);
    Task<bool> IsUserOnlineAsync(string userId);
}