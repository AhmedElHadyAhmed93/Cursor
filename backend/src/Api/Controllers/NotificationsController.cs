using Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a test notification to current user
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification([FromBody] NotificationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        await _notificationService.SendPushNotificationAsync(userId, request.Title, request.Body, request.Data);
        
        _logger.LogInformation("Test notification sent to user {UserId}", userId);
        
        return Ok(new { message = "Test notification sent" });
    }

    /// <summary>
    /// Send a notification to a specific user (Admin only)
    /// </summary>
    [HttpPost("user/{userId}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> SendNotificationToUser(string userId, [FromBody] NotificationRequest request)
    {
        await _notificationService.SendPushNotificationAsync(userId, request.Title, request.Body, request.Data);
        
        _logger.LogInformation("Notification sent to user {UserId} by {SenderId}", userId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        
        return Ok(new { message = "Notification sent to user" });
    }

    /// <summary>
    /// Send a broadcast notification to all users (Admin only)
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> BroadcastNotification([FromBody] NotificationRequest request)
    {
        await _notificationService.SendPushNotificationToAllAsync(request.Title, request.Body, request.Data);
        
        _logger.LogInformation("Broadcast notification sent by {SenderId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        
        return Ok(new { message = "Broadcast notification sent" });
    }

    /// <summary>
    /// Send email notification (Admin only)
    /// </summary>
    [HttpPost("email")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        await _notificationService.SendEmailAsync(request.To, request.Subject, request.Body);
        
        _logger.LogInformation("Email sent to {Email} by {SenderId}", request.To, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        
        return Ok(new { message = "Email sent" });
    }

    /// <summary>
    /// Send SMS notification (Admin only)
    /// </summary>
    [HttpPost("sms")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> SendSms([FromBody] SmsRequest request)
    {
        await _notificationService.SendSmsAsync(request.PhoneNumber, request.Message);
        
        _logger.LogInformation("SMS sent to {PhoneNumber} by {SenderId}", request.PhoneNumber, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        
        return Ok(new { message = "SMS sent" });
    }
}