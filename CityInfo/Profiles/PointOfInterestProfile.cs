using AutoMapper;
using CityInfo.Entities;
using CityInfo.Models;

namespace CityInfo.Profiles
{
    public class PointOfInterestProfile : Profile
    {
        public PointOfInterestProfile()
        {
            CreateMap<PointOfInterest, PointsOfInterestDto>();
            CreateMap<PointOfInterestForCreationDto, PointOfInterest>();
            CreateMap<PointOfInterestForUpdateDto, PointOfInterest>();
            CreateMap<PointOfInterest, PointOfInterestForUpdateDto>();

        }
    }
}
