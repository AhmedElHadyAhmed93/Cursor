using Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Identity.Services;

public class IdentitySeederService : IIdentitySeederService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeederService> _logger;

    public IdentitySeederService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<IdentitySeederService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedSuperAdminAsync();
        await SeedClaimsAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "SuperAdmin", "Admin", "User" };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                var result = await _roleManager.CreateAsync(role);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}", 
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedSuperAdminAsync()
    {
        var adminEmail = _configuration["SEED:ADMIN:EMAIL"] ?? "Admin@mail.com";
        var adminPassword = _configuration["SEED:ADMIN:PASSWORD"] ?? "P@ssw0rd";

        var existingUser = await _userManager.FindByEmailAsync(adminEmail);
        if (existingUser != null)
        {
            _logger.LogInformation("SuperAdmin user already exists: {Email}", adminEmail);
            return;
        }

        var superAdmin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true
        };

        var result = await _userManager.CreateAsync(superAdmin, adminPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRolesAsync(superAdmin, new[] { "SuperAdmin", "Admin" });
            
            _logger.LogInformation("Created SuperAdmin user: {Email}", adminEmail);
        }
        else
        {
            _logger.LogError("Failed to create SuperAdmin user: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedClaimsAsync()
    {
        var claimsToSeed = new[]
        {
            "CanManageUsers",
            "CanManageRoles",
            "CanManageCars",
            "CanViewAuditLogs",
            "CanManageSystem"
        };

        var superAdminRole = await _roleManager.FindByNameAsync("SuperAdmin");
        if (superAdminRole != null)
        {
            foreach (var claimValue in claimsToSeed)
            {
                var existingClaim = await _roleManager.GetClaimsAsync(superAdminRole);
                if (!existingClaim.Any(c => c.Value == claimValue))
                {
                    await _roleManager.AddClaimAsync(superAdminRole, new Claim("permission", claimValue));
                    _logger.LogInformation("Added claim {ClaimValue} to SuperAdmin role", claimValue);
                }
            }
        }

        var adminRole = await _roleManager.FindByNameAsync("Admin");
        if (adminRole != null)
        {
            var adminClaims = new[] { "CanManageUsers", "CanManageCars" };
            foreach (var claimValue in adminClaims)
            {
                var existingClaim = await _roleManager.GetClaimsAsync(adminRole);
                if (!existingClaim.Any(c => c.Value == claimValue))
                {
                    await _roleManager.AddClaimAsync(adminRole, new Claim("permission", claimValue));
                    _logger.LogInformation("Added claim {ClaimValue} to Admin role", claimValue);
                }
            }
        }
    }
}