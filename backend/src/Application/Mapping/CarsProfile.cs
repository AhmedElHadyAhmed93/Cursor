using Application.DTOs.Cars;
using AutoMapper;
using Core.Entities;

namespace Application.Mapping;

public class CarsProfile : Profile
{
    public CarsProfile()
    {
        CreateMap<Car, CarDto>();
        CreateMap<Car, CarDetailDto>()
            .ForMember(dest => dest.Owners, opt => opt.MapFrom(src => src.OwnerCars));
        
        CreateMap<CreateCarDto, Car>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        
        CreateMap<UpdateCarDto, Car>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<OwnerCar, OwnerCarDto>()
            .ForMember(dest => dest.OwnerName, opt => opt.Ignore()); // Will be populated by service
    }
}