using Application.DTOs.Cars;
using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Identity.Entities;
using Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using Api.Hubs;

namespace Application.Services.Cars;

public class CarService : CrudBaseService<Car, CarDto, CreateCarDto, UpdateCarDto>, ICarService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<SocketHub> _hubContext;

    public CarService(
        IRepository<Car> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CarService> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<SocketHub> hubContext)
        : base(repository, unitOfWork, mapper, logger)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    public async Task<CarDetailDto?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _context.Cars
            .Include(c => c.OwnerCars)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (car == null) return null;

        var carDetail = _mapper.Map<CarDetailDto>(car);

        // Populate owner names
        foreach (var ownerCar in carDetail.Owners)
        {
            var user = await _userManager.FindByIdAsync(ownerCar.OwnerId);
            ownerCar.OwnerName = user?.FullName ?? "Unknown User";
        }

        return carDetail;
    }

    public async Task AssignOwnerAsync(int carId, string userId, string ownershipType, CancellationToken cancellationToken = default)
    {
        var car = await _repository.GetByIdAsync(carId, cancellationToken);
        if (car == null)
        {
            throw new KeyNotFoundException($"Car with ID {carId} not found");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Check if already assigned
        var existingAssignment = await _context.OwnerCars
            .FirstOrDefaultAsync(oc => oc.CarId == carId && oc.OwnerId == userId, cancellationToken);

        if (existingAssignment != null)
        {
            throw new InvalidOperationException("User is already assigned to this car");
        }

        var ownerCar = new OwnerCar
        {
            CarId = carId,
            OwnerId = userId,
            OwnershipType = ownershipType,
            AssignedAt = DateTime.UtcNow
        };

        _context.OwnerCars.Add(ownerCar);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned user {UserId} to car {CarId} with ownership type {OwnershipType}", 
            userId, carId, ownershipType);

        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("CarUpdated", new { CarId = carId, Action = "OwnerAssigned", UserId = userId }, cancellationToken);
    }

    public async Task UnassignOwnerAsync(int carId, string userId, CancellationToken cancellationToken = default)
    {
        var ownerCar = await _context.OwnerCars
            .FirstOrDefaultAsync(oc => oc.CarId == carId && oc.OwnerId == userId, cancellationToken);

        if (ownerCar == null)
        {
            throw new KeyNotFoundException("Owner assignment not found");
        }

        _context.OwnerCars.Remove(ownerCar);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Unassigned user {UserId} from car {CarId}", userId, carId);

        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("CarUpdated", new { CarId = carId, Action = "OwnerUnassigned", UserId = userId }, cancellationToken);
    }

    public async Task<PaginatedResult<CarDto>> GetCarsByOwnerAsync(string userId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Cars
            .Where(c => c.OwnerCars.Any(oc => oc.OwnerId == userId))
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(c => 
                c.Make.Contains(request.SearchTerm) ||
                c.Model.Contains(request.SearchTerm) ||
                c.Vin.Contains(request.SearchTerm));
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(request.SortBy))
        {
            query = request.SortBy.ToLower() switch
            {
                "make" => request.SortDescending ? query.OrderByDescending(c => c.Make) : query.OrderBy(c => c.Make),
                "model" => request.SortDescending ? query.OrderByDescending(c => c.Model) : query.OrderBy(c => c.Model),
                "year" => request.SortDescending ? query.OrderByDescending(c => c.Year) : query.OrderBy(c => c.Year),
                _ => request.SortDescending ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<CarDto>>(items);

        return new PaginatedResult<CarDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    protected override IQueryable<Car> ApplySearchFilter(IQueryable<Car> query, string searchTerm)
    {
        return query.Where(c => 
            c.Make.Contains(searchTerm) ||
            c.Model.Contains(searchTerm) ||
            c.Vin.Contains(searchTerm));
    }

    protected override IQueryable<Car> ApplySorting(IQueryable<Car> query, string sortBy, bool descending)
    {
        return sortBy.ToLower() switch
        {
            "make" => descending ? query.OrderByDescending(c => c.Make) : query.OrderBy(c => c.Make),
            "model" => descending ? query.OrderByDescending(c => c.Model) : query.OrderBy(c => c.Model),
            "year" => descending ? query.OrderByDescending(c => c.Year) : query.OrderBy(c => c.Year),
            "vin" => descending ? query.OrderByDescending(c => c.Vin) : query.OrderBy(c => c.Vin),
            _ => descending ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
        };
    }

    public override async Task<CarDto> CreateAsync(CreateCarDto createDto, CancellationToken cancellationToken = default)
    {
        var result = await base.CreateAsync(createDto, cancellationToken);
        
        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("CarUpdated", new { CarId = result.Id, Action = "Created" }, cancellationToken);
        
        return result;
    }

    public override async Task<CarDto> UpdateAsync(int id, UpdateCarDto updateDto, CancellationToken cancellationToken = default)
    {
        var result = await base.UpdateAsync(id, updateDto, cancellationToken);
        
        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("CarUpdated", new { CarId = id, Action = "Updated" }, cancellationToken);
        
        return result;
    }

    public override async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(id, cancellationToken);
        
        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("CarUpdated", new { CarId = id, Action = "Deleted" }, cancellationToken);
    }
}