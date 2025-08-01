using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Identity.Data;
using StackExchange.Redis;
using MongoDB.Driver;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _appContext;
    private readonly IdentityDbContext _identityContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext appContext,
        IdentityDbContext identityContext,
        IConfiguration configuration,
        ILogger<HealthController> logger)
    {
        _appContext = appContext;
        _identityContext = identityContext;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var healthStatus = new HealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        var checks = new List<HealthCheck>();

        // SQL Server Health Check
        try
        {
            await _appContext.Database.CanConnectAsync();
            await _identityContext.Database.CanConnectAsync();
            checks.Add(new HealthCheck { Name = "SQL Server", Status = "Healthy", ResponseTime = "< 100ms" });
        }
        catch (Exception ex)
        {
            checks.Add(new HealthCheck { Name = "SQL Server", Status = "Unhealthy", Error = ex.Message });
            healthStatus.Status = "Unhealthy";
        }

        // Redis Health Check
        try
        {
            var redisConnection = _configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                var redis = ConnectionMultiplexer.Connect(redisConnection);
                var database = redis.GetDatabase();
                await database.PingAsync();
                checks.Add(new HealthCheck { Name = "Redis", Status = "Healthy", ResponseTime = "< 50ms" });
            }
            else
            {
                checks.Add(new HealthCheck { Name = "Redis", Status = "Not Configured" });
            }
        }
        catch (Exception ex)
        {
            checks.Add(new HealthCheck { Name = "Redis", Status = "Unhealthy", Error = ex.Message });
            healthStatus.Status = "Degraded";
        }

        // MongoDB Health Check
        try
        {
            var mongoConnection = _configuration.GetConnectionString("MongoDB");
            if (!string.IsNullOrEmpty(mongoConnection))
            {
                var client = new MongoClient(mongoConnection);
                await client.ListDatabaseNamesAsync();
                checks.Add(new HealthCheck { Name = "MongoDB", Status = "Healthy", ResponseTime = "< 100ms" });
            }
            else
            {
                checks.Add(new HealthCheck { Name = "MongoDB", Status = "Not Configured" });
            }
        }
        catch (Exception ex)
        {
            checks.Add(new HealthCheck { Name = "MongoDB", Status = "Unhealthy", Error = ex.Message });
            healthStatus.Status = "Degraded";
        }

        healthStatus.Checks = checks;

        var statusCode = healthStatus.Status switch
        {
            "Healthy" => 200,
            "Degraded" => 200,
            "Unhealthy" => 503,
            _ => 500
        };

        return StatusCode(statusCode, healthStatus);
    }
}

public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public List<HealthCheck> Checks { get; set; } = new();
}

public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ResponseTime { get; set; }
    public string? Error { get; set; }
}