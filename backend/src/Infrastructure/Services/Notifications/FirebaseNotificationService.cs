using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Notifications;

public class FirebaseNotificationService : INotificationService
{
    private readonly FirebaseMessaging _messaging;
    private readonly IdentityDbContext _identityContext;
    private readonly ILogger<FirebaseNotificationService> _logger;

    public FirebaseNotificationService(
        IConfiguration configuration,
        IdentityDbContext identityContext,
        ILogger<FirebaseNotificationService> logger)
    {
        _identityContext = identityContext;
        _logger = logger;

        try
        {
            var credentialsPath = configuration["Firebase:CredentialsPath"];
            if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
            {
                var app = FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(credentialsPath),
                    ProjectId = configuration["Firebase:ProjectId"]
                });
                _messaging = FirebaseMessaging.GetMessaging(app);
            }
            else
            {
                _logger.LogWarning("Firebase credentials not found. Push notifications will not work.");
                _messaging = null!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase");
            _messaging = null!;
        }
    }

    public async Task SendPushNotificationAsync(string userId, string title, string body, Dictionary<string, string>? data = null)
    {
        if (_messaging == null)
        {
            _logger.LogWarning("Firebase not initialized. Cannot send push notification.");
            return;
        }

        try
        {
            var user = await _identityContext.Users.FindAsync(userId);
            if (user?.FirebaseToken == null)
            {
                _logger.LogWarning("User {UserId} has no Firebase token", userId);
                return;
            }

            var message = new Message()
            {
                Token = user.FirebaseToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await _messaging.SendAsync(message);
            _logger.LogInformation("Successfully sent message to user {UserId}: {Response}", userId, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
        }
    }

    public async Task SendPushNotificationToAllAsync(string title, string body, Dictionary<string, string>? data = null)
    {
        if (_messaging == null)
        {
            _logger.LogWarning("Firebase not initialized. Cannot send push notification.");
            return;
        }

        try
        {
            var usersWithTokens = await _identityContext.Users
                .Where(u => !string.IsNullOrEmpty(u.FirebaseToken))
                .Select(u => u.FirebaseToken!)
                .ToListAsync();

            if (!usersWithTokens.Any())
            {
                _logger.LogWarning("No users with Firebase tokens found");
                return;
            }

            var message = new MulticastMessage()
            {
                Tokens = usersWithTokens,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await _messaging.SendMulticastAsync(message);
            _logger.LogInformation("Successfully sent message to {SuccessCount} users, {FailureCount} failures", 
                response.SuccessCount, response.FailureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to all users");
        }
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        // Placeholder for email service integration
        _logger.LogInformation("Email would be sent to {Email} with subject: {Subject}", email, subject);
        await Task.CompletedTask;
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        // Placeholder for SMS service integration
        _logger.LogInformation("SMS would be sent to {PhoneNumber}: {Message}", phoneNumber, message);
        await Task.CompletedTask;
    }
}