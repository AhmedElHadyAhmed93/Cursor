using Identity.Data;
using Infrastructure.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Background;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly INotificationService _notificationService;
    private readonly IdentityDbContext _identityContext;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        INotificationService notificationService,
        IdentityDbContext identityContext,
        ILogger<BackgroundJobService> logger)
    {
        _notificationService = notificationService;
        _identityContext = identityContext;
        _logger = logger;
    }

    public async Task SendMonthlyNotificationToAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Starting monthly notification job");

            var title = "Monthly Update";
            var body = $"Hello! Here's your monthly update for {DateTime.Now:MMMM yyyy}. Thank you for using our service!";
            var data = new Dictionary<string, string>
            {
                ["type"] = "monthly_update",
                ["month"] = DateTime.Now.ToString("yyyy-MM")
            };

            await _notificationService.SendPushNotificationToAllAsync(title, body, data);

            _logger.LogInformation("Monthly notification job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send monthly notifications");
            throw;
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        try
        {
            _logger.LogInformation("Starting token cleanup job");

            var expiredTokens = await _identityContext.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow || rt.RevokedAt != null)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _identityContext.RefreshTokens.RemoveRange(expiredTokens);
                var deletedCount = await _identityContext.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned up {Count} expired/revoked tokens", deletedCount);
            }
            else
            {
                _logger.LogInformation("No expired tokens found to cleanup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired tokens");
            throw;
        }
    }

    public async Task GenerateMonthlyReportsAsync()
    {
        try
        {
            _logger.LogInformation("Starting monthly report generation job");

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // Example: Count new users this month
            var newUsersCount = await _identityContext.Users
                .Where(u => u.CreatedAt.Month == currentMonth && u.CreatedAt.Year == currentYear)
                .CountAsync();

            // Example: Count active users (logged in this month)
            var activeUsersCount = await _identityContext.Users
                .Where(u => u.LastLoginAt.HasValue && 
                           u.LastLoginAt.Value.Month == currentMonth && 
                           u.LastLoginAt.Value.Year == currentYear)
                .CountAsync();

            _logger.LogInformation("Monthly Report - New Users: {NewUsers}, Active Users: {ActiveUsers}", 
                newUsersCount, activeUsersCount);

            // Here you could save the report to database, send email, etc.
            
            _logger.LogInformation("Monthly report generation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate monthly reports");
            throw;
        }
    }
}