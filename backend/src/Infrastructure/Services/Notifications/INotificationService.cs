using Core.Interfaces.Services;

namespace Infrastructure.Services.Notifications;

public interface INotificationService : IScopedService
{
    Task SendPushNotificationAsync(string userId, string title, string body, Dictionary<string, string>? data = null);
    Task SendPushNotificationToAllAsync(string title, string body, Dictionary<string, string>? data = null);
    Task SendEmailAsync(string email, string subject, string body);
    Task SendSmsAsync(string phoneNumber, string message);
}

public class NotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
}

public class EmailRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = false;
}

public class SmsRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}