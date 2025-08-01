using Application.DTOs.Cars;
using Application.Services.Cars;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarsController : ControllerBase
{
    private readonly ICarService _carService;
    private readonly ILogger<CarsController> _logger;

    public CarsController(ICarService carService, ILogger<CarsController> logger)
    {
        _carService = carService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of cars
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<CarDto>>> GetCars([FromQuery] PaginationRequest request)
    {
        var result = await _carService.GetPagedAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get car by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CarDto>> GetCar(int id)
    {
        var car = await _carService.GetByIdAsync(id);
        if (car == null)
        {
            return NotFound();
        }
        return Ok(car);
    }

    /// <summary>
    /// Get car details with owners
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<ActionResult<CarDetailDto>> GetCarDetails(int id)
    {
        var car = await _carService.GetDetailByIdAsync(id);
        if (car == null)
        {
            return NotFound();
        }
        return Ok(car);
    }

    /// <summary>
    /// Create a new car
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanManageCars")]
    public async Task<ActionResult<CarDto>> CreateCar(CreateCarDto createCarDto)
    {
        var car = await _carService.CreateAsync(createCarDto);
        return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
    }

    /// <summary>
    /// Update an existing car
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageCars")]
    public async Task<ActionResult<CarDto>> UpdateCar(int id, UpdateCarDto updateCarDto)
    {
        try
        {
            var car = await _carService.UpdateAsync(id, updateCarDto);
            return Ok(car);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Delete a car
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageCars")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        try
        {
            await _carService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Assign an owner to a car
    /// </summary>
    [HttpPost("{id}/owners/{userId}")]
    [Authorize(Policy = "CanManageCars")]
    public async Task<IActionResult> AssignOwner(int id, string userId, [FromBody] AssignOwnerDto assignOwnerDto)
    {
        try
        {
            await _carService.AssignOwnerAsync(id, userId, assignOwnerDto.OwnershipType);
            return Ok(new { message = "Owner assigned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Unassign an owner from a car
    /// </summary>
    [HttpDelete("{id}/owners/{userId}")]
    [Authorize(Policy = "CanManageCars")]
    public async Task<IActionResult> UnassignOwner(int id, string userId)
    {
        try
        {
            await _carService.UnassignOwnerAsync(id, userId);
            return Ok(new { message = "Owner unassigned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get cars owned by current user
    /// </summary>
    [HttpGet("my-cars")]
    public async Task<ActionResult<PaginatedResult<CarDto>>> GetMyCars([FromQuery] PaginationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _carService.GetCarsByOwnerAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// Get cars owned by specific user
    /// </summary>
    [HttpGet("by-owner/{userId}")]
    [Authorize(Policy = "CanManageCars")]
    public async Task<ActionResult<PaginatedResult<CarDto>>> GetCarsByOwner(string userId, [FromQuery] PaginationRequest request)
    {
        var result = await _carService.GetCarsByOwnerAsync(userId, request);
        return Ok(result);
    }
}

public class AssignOwnerDto
{
    public string OwnershipType { get; set; } = "Owner";
}