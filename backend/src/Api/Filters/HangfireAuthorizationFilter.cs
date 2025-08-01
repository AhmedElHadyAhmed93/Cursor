using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;

namespace Api.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IConfiguration _configuration;

    public HangfireAuthorizationFilter()
    {
        // This will be resolved from DI in a real implementation
        // For now, we'll use a simple approach
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // In development, allow all
        if (httpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
        {
            return true;
        }

        // Check if user is authenticated and has admin role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               (httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("SuperAdmin"));
    }
}