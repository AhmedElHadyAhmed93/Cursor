using Core.Interfaces.Services;

namespace Infrastructure.Services.Background;

public interface IBackgroundJobService : IScopedService
{
    Task SendMonthlyNotificationToAllUsersAsync();
    Task CleanupExpiredTokensAsync();
    Task GenerateMonthlyReportsAsync();
}