using Core.Entities;
using Core.Interfaces.Services;
using Shared.DTOs;

namespace Application.Services;

public interface ICrudBaseService<TEntity, TDto, TCreateDto, TUpdateDto> : IScopedService
    where TEntity : BaseEntity
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    Task<TDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaginatedResult<TDto>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default);
    Task<TDto> UpdateAsync(int id, TUpdateDto updateDto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}