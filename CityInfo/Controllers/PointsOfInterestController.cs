using Asp.Versioning;
using AutoMapper;
using CityInfo.Entities;
using CityInfo.Models;
using CityInfo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace CityInfo.Controllers
{
    [Route("api/v{version:apiVersion}/cities/{cityId}/pointsOfInterest")]
    [ApiController]
    [ApiVersion("2.0")]
    [Authorize(Policy = "MustBeFromParis")]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger;
        private readonly IMailService _mailService;
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;

        public PointsOfInterestController(ILogger<PointsOfInterestController> logger, IMailService mailService, ICityInfoRepository cityInfoRepository, IMapper mapper)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
            _cityInfoRepository = cityInfoRepository ?? throw new ArgumentException(nameof(cityInfoRepository));
            _mapper = mapper ?? throw new ArgumentException(nameof(mapper));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointsOfInterestDto>>> GetPointsOfInterest(int cityId)
        {
            var cityName = User.Claims.FirstOrDefault(c => c.Type == "city")?.Value;

            if (!await _cityInfoRepository.CityNameMatchesCityId(cityName, cityId))
            {
                return Forbid();
            }

            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                _logger.LogInformation($"City with id {cityId} wasn't found when accessing points of interest.");
                return NotFound();
            }

           var pointsOfInterestForCity = await _cityInfoRepository.GetPointsOfInterestForCityAsync(cityId);

            return Ok(_mapper.Map<IEnumerable<PointsOfInterestDto>>(pointsOfInterestForCity));
        }


        [HttpGet("{pointOfInterestId}", Name = "GetPointOfInterest")]
        public async Task<ActionResult<PointsOfInterestDto>> GetPointOfInterest(int cityId, int pointOfInterestId)
        {

            if (! await _cityInfoRepository.CityExistsAsync(cityId))
            {
                _logger.LogInformation($"City with id {cityId} wasn't found when accessing points of interest");
                return NotFound();
            }

            var pointsOfInterest = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointsOfInterest == null)
            {
                _logger.LogInformation($"Point of interest id {pointOfInterestId} wasn't found");
                return NotFound();
            }

            return Ok(_mapper.Map<PointsOfInterestDto>(pointsOfInterest));
        }
        
        [HttpPost]
        public async Task<ActionResult <PointsOfInterestDto>> CreatePointOfInterest(int cityId, PointOfInterestForCreationDto pointOfInterest)
        {

            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                _logger.LogInformation($"City with id {cityId} wasn't found when accessing points of interest");
                return NotFound();
            }

            var finalPointOfInterest = _mapper.Map<PointOfInterest>(pointOfInterest);

            await _cityInfoRepository.AddPointOfInterestForCityAsync(cityId, finalPointOfInterest);

            await _cityInfoRepository.SaveChangesAsync();

            var createdPointsOfInterestReturn = _mapper.Map<PointsOfInterestDto>(finalPointOfInterest);

            return CreatedAtRoute("GetPointOfInterest", 
                new
                {
                    cityId = cityId,
                    pointOfInterestId = createdPointsOfInterestReturn.Id
                }, createdPointsOfInterestReturn);
        }


        [HttpPut("{pointOfInterestId}")]
        public async Task<IActionResult> UpdatePointOfInterest(int cityId, int pointOfInterestId, PointOfInterestForUpdateDto pointOfInterest)
        {
           
            if (! await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity= await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _mapper.Map(pointOfInterest, pointOfInterestEntity);
            await _cityInfoRepository.SaveChangesAsync();
            
            return NoContent();

        }

        [HttpPatch("{pointOfInterestId}")]
        public async Task<ActionResult> PartiallyUpdatePointOfInterest(int cityId, int pointOfInterestId, JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {

            if (! await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }
            
            var pointOfInterestToPatch = _mapper.Map<PointOfInterestForUpdateDto>(pointOfInterestEntity);

            patchDocument.ApplyTo(pointOfInterestToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }

            _mapper.Map(pointOfInterestToPatch, pointOfInterestEntity);
            await _cityInfoRepository.SaveChangesAsync();
        
            
            return NoContent();
        }

        [HttpDelete("{pointOfInterestId}")]
        public async Task<ActionResult> DeletePointOfInterestAsync(int cityId, int pointOfInterestId)
        {

            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity= await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _cityInfoRepository.DeletePointOfInterestForCityAsync(pointOfInterestEntity);
            await _cityInfoRepository.SaveChangesAsync();

            _mailService.Send("Point of interest deleted.", $"Point of interest {pointOfInterestEntity.Name} with id {pointOfInterestEntity.Id}");

            return NoContent();
        } 
    }
}
