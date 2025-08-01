using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class CrudBaseService<TEntity, TDto, TCreateDto, TUpdateDto> : ICrudBaseService<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : BaseEntity
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IMapper _mapper;
    protected readonly ILogger<CrudBaseService<TEntity, TDto, TCreateDto, TUpdateDto>> _logger;

    public CrudBaseService(
        IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CrudBaseService<TEntity, TDto, TCreateDto, TUpdateDto>> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public virtual async Task<TDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : _mapper.Map<TDto>(entity);
    }

    public virtual async Task<PaginatedResult<TDto>> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = await GetQueryableAsync();

        // Apply search filter if provided
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = ApplySearchFilter(query, request.SearchTerm);
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(request.SortBy))
        {
            query = ApplySorting(query, request.SortBy, request.SortDescending);
        }

        var totalCount = query.Count();
        
        var items = query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = _mapper.Map<List<TDto>>(items);

        return new PaginatedResult<TDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public virtual async Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<TEntity>(createDto);
        
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {EntityType} with ID {Id}", typeof(TEntity).Name, entity.Id);

        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task<TDto> UpdateAsync(int id, TUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new KeyNotFoundException($"{typeof(TEntity).Name} with ID {id} not found");
        }

        _mapper.Map(updateDto, entity);
        
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated {EntityType} with ID {Id}", typeof(TEntity).Name, id);

        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new KeyNotFoundException($"{typeof(TEntity).Name} with ID {id} not found");
        }

        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {EntityType} with ID {Id}", typeof(TEntity).Name, id);
    }

    protected virtual async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        var entities = await _repository.ListAllAsync();
        return entities.AsQueryable();
    }

    protected virtual IQueryable<TEntity> ApplySearchFilter(IQueryable<TEntity> query, string searchTerm)
    {
        // Override in derived classes to implement entity-specific search
        return query;
    }

    protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, string sortBy, bool descending)
    {
        // Basic sorting by Id if no specific sorting is implemented
        return descending ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id);
    }
}