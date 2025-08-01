using Application.DTOs.Cars;
using Core.Entities;
using Core.Interfaces.Services;
using Shared.DTOs;

namespace Application.Services.Cars;

public interface ICarService : ICrudBaseService<Car, CarDto, CreateCarDto, UpdateCarDto>, IScopedService
{
    Task<CarDetailDto?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AssignOwnerAsync(int carId, string userId, string ownershipType, CancellationToken cancellationToken = default);
    Task UnassignOwnerAsync(int carId, string userId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<CarDto>> GetCarsByOwnerAsync(string userId, PaginationRequest request, CancellationToken cancellationToken = default);
}