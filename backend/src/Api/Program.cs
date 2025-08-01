using Api.Extensions;
using Api.Filters;
using Api.Hubs;
using Api.Middleware;
using Identity.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting web application");

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Configure all services
    builder.Services.ConfigureDatabase(builder.Configuration);
    builder.Services.ConfigureIdentity();
    builder.Services.ConfigureJwtAuthentication(builder.Configuration);
    builder.Services.ConfigureSwagger();
    builder.Services.ConfigureAutoMapper();
    builder.Services.ConfigureCors(builder.Configuration);
    builder.Services.ConfigureRateLimiting(builder.Configuration);
    builder.Services.ConfigureSignalR(builder.Configuration);
    builder.Services.ConfigureHangfire(builder.Configuration);
    builder.Services.ConfigureValidation();
    builder.Services.RegisterApplicationServices();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
            c.OAuthClientId("swagger");
            c.OAuthAppName("Swagger UI");
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("DefaultPolicy");
    app.UseRateLimiting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.MapControllers();
    app.MapHub<SocketHub>("/hubs/socket");

    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    // Configure Hangfire recurring jobs
    app.ConfigureHangfireJobs();

    // Seed data
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeederService>();
        await seeder.SeedAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}