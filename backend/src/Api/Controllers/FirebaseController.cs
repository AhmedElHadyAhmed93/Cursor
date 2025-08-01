using Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FirebaseController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<FirebaseController> _logger;

    public FirebaseController(UserManager<ApplicationUser> userManager, ILogger<FirebaseController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Update Firebase token for current user
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> UpdateFirebaseToken([FromBody] UpdateFirebaseTokenRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.FirebaseToken = request.Token;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("Firebase token updated for user {UserId}", userId);
            return Ok(new { message = "Firebase token updated successfully" });
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Remove Firebase token for current user
    /// </summary>
    [HttpDelete("token")]
    public async Task<IActionResult> RemoveFirebaseToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.FirebaseToken = null;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("Firebase token removed for user {UserId}", userId);
            return Ok(new { message = "Firebase token removed successfully" });
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Get current Firebase token status
    /// </summary>
    [HttpGet("token")]
    public async Task<IActionResult> GetFirebaseTokenStatus()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new { hasToken = !string.IsNullOrEmpty(user.FirebaseToken) });
    }
}

public class UpdateFirebaseTokenRequest
{
    public string Token { get; set; } = string.Empty;
}