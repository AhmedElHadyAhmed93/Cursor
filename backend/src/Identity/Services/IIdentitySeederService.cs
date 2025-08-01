using Core.Interfaces.Services;

namespace Identity.Services;

public interface IIdentitySeederService : IScopedService
{
    Task SeedAsync();
}