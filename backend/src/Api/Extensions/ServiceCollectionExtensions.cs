using Application.Services;
using Core.Interfaces;
using Core.Interfaces.Services;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Identity.Data;
using Identity.Entities;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Infrastructure.Repositories;
using Infrastructure.Services.Audit;
using Infrastructure.Services.Background;
using Infrastructure.Services.Notifications;
using Infrastructure.Services.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using AspNetCoreRateLimit;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") 
            ?? throw new InvalidOperationException("Database connection string not found");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var key = Encoding.ASCII.GetBytes(jwtKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["JWT:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // SignalR support
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin", "SuperAdmin"));
            options.AddPolicy("RequireSuperAdminRole", policy => policy.RequireRole("SuperAdmin"));
            options.AddPolicy("CanManageCars", policy => policy.RequireClaim("permission", "CanManageCars"));
            options.AddPolicy("CanManageUsers", policy => policy.RequireClaim("permission", "CanManageUsers"));
            options.AddPolicy("CanViewAuditLogs", policy => policy.RequireClaim("permission", "CanViewAuditLogs"));
        });

        return services;
    }

    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Clean Architecture API",
                Version = "v1",
                Description = "A production-ready API with .NET 8, Clean Architecture, and comprehensive features"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        return services;
    }

    public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                    ?? new[] { "http://localhost:4200", "https://localhost:4200" };

                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    public static IServiceCollection ConfigureSignalR(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        
        var signalRBuilder = services.AddSignalR();
        
        if (!string.IsNullOrEmpty(redisConnection))
        {
            signalRBuilder.AddStackExchangeRedis(redisConnection);
        }

        return services;
    }

    public static IServiceCollection ConfigureHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        
        services.AddHangfire(config =>
        {
            config.UseSqlServerStorage(connectionString);
        });

        services.AddHangfireServer();

        return services;
    }

    public static IServiceCollection ConfigureValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Application.AssemblyMarker).Assembly);
        return services;
    }

    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        // Register services by lifecycle marker interfaces using reflection
        var assemblies = new[]
        {
            typeof(Core.AssemblyMarker).Assembly,
            typeof(Application.AssemblyMarker).Assembly,
            typeof(Infrastructure.AssemblyMarker).Assembly,
            typeof(Identity.AssemblyMarker).Assembly
        };

        foreach (var assembly in assemblies)
        {
            // Register Scoped services
            var scopedServices = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IScopedService).IsAssignableFrom(t))
                .ToList();

            foreach (var service in scopedServices)
            {
                var interfaces = service.GetInterfaces()
                    .Where(i => i != typeof(IScopedService) && typeof(IScopedService).IsAssignableFrom(i))
                    .ToList();

                if (interfaces.Any())
                {
                    foreach (var @interface in interfaces)
                    {
                        services.AddScoped(@interface, service);
                    }
                }
                else
                {
                    services.AddScoped(service);
                }
            }

            // Register Singleton services
            var singletonServices = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISingletonService).IsAssignableFrom(t))
                .ToList();

            foreach (var service in singletonServices)
            {
                var interfaces = service.GetInterfaces()
                    .Where(i => i != typeof(ISingletonService) && typeof(ISingletonService).IsAssignableFrom(i))
                    .ToList();

                if (interfaces.Any())
                {
                    foreach (var @interface in interfaces)
                    {
                        services.AddSingleton(@interface, service);
                    }
                }
                else
                {
                    services.AddSingleton(service);
                }
            }

            // Register Transient services
            var transientServices = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ITransientService).IsAssignableFrom(t))
                .ToList();

            foreach (var service in transientServices)
            {
                var interfaces = service.GetInterfaces()
                    .Where(i => i != typeof(ITransientService) && typeof(ITransientService).IsAssignableFrom(i))
                    .ToList();

                if (interfaces.Any())
                {
                    foreach (var @interface in interfaces)
                    {
                        services.AddTransient(@interface, service);
                    }
                }
                else
                {
                    services.AddTransient(service);
                }
            }
        }

        // Manual registrations for specific services
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpContextAccessor();

        // Register specific implementations
        services.AddScoped<INotificationService, FirebaseNotificationService>();
        services.AddScoped<IConnectionTrackingService, RedisConnectionTrackingService>();

        return services;
    }

    public static void ConfigureHangfireJobs(this IApplicationBuilder app)
    {
        // Schedule recurring jobs
        RecurringJob.AddOrUpdate<IBackgroundJobService>(
            "monthly-notifications",
            service => service.SendMonthlyNotificationToAllUsersAsync(),
            "0 0 1 * *"); // First day of every month at midnight

        RecurringJob.AddOrUpdate<IBackgroundJobService>(
            "cleanup-expired-tokens",
            service => service.CleanupExpiredTokensAsync(),
            "0 2 * * *"); // Every day at 2 AM

        RecurringJob.AddOrUpdate<IBackgroundJobService>(
            "monthly-reports",
            service => service.GenerateMonthlyReportsAsync(),
            "0 1 1 * *"); // First day of every month at 1 AM
    }
}