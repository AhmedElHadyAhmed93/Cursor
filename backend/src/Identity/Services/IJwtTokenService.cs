using Identity.DTOs;
using Identity.Entities;
using Core.Interfaces.Services;

namespace Identity.Services;

public interface IJwtTokenService : IScopedService
{
    Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(string userId);
}