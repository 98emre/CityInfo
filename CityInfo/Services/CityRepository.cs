using CityInfo.DbContexts;
using CityInfo.Entities;
using Microsoft.EntityFrameworkCore;

namespace CityInfo.Services
{
    public class CityRepository : ICityInfoRepository
    {
        private readonly CityInfoContext _context;

        public CityRepository(CityInfoContext context)
        {
            _context = context ?? throw new ArgumentException(nameof(context)); 
        }

        public async Task<IEnumerable<City>> GetCitiesAsync()
        {
            return await _context.Cities.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<(IEnumerable<City>, PaginationMetadata)> GetCitiesAsync(string? name, string? searchQuery, int pageNumber ,int pageSize)
        {

            var collection = _context.Cities as IQueryable<City>;

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                collection = collection.Where(c => c.Name == name);
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.Trim();
                collection = collection.Where(c => c.Name.Contains(searchQuery) || (c.Description !=null && c.Description.Contains(searchQuery)));
            }

            var totalItemCount = await collection.CountAsync();
            var paginationMetadata = new PaginationMetadata(totalItemCount, pageSize, pageNumber);

            var collectionToReturn = await collection
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();

            return (collectionToReturn, paginationMetadata);
        }


        public async Task<City?> GetCityAsync(int cityId, bool includePointsOfInterest)
        {
            if (includePointsOfInterest) {
                return await _context.Cities
                    .Include(c => c.PointOfInterest)
                    .Where(c => c.Id == cityId)
                    .FirstOrDefaultAsync();  
            }

            return await _context.Cities
                  .Where(c => c.Id == cityId)
                  .FirstOrDefaultAsync();
        }

        public async Task<PointOfInterest> GetPointOfInterestForCityAsync(int cityId, int pointOfInterestId)
        {
            return await _context.PointOfInterests
                   .Where(p => p.CityId == cityId && p.Id == pointOfInterestId)
                   .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityId)
        {
            return await _context.PointOfInterests
                  .Where(p => p.CityId == cityId)
                  .ToListAsync();
        }

        public async Task<bool> CityExistsAsync(int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId);
        }

        public async Task AddPointOfInterestForCityAsync(int cityId, PointOfInterest pointOfInterest)
        {
            var city = await GetCityAsync(cityId, false);

            if (city != null)
            {
                city.PointOfInterest.Add(pointOfInterest);
            }

        }

        public void DeletePointOfInterestForCityAsync(PointOfInterest pointOfInterest)
        {
            _context.PointOfInterests.Remove(pointOfInterest);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() > 0);
        }

        public async Task<bool> CityNameMatchesCityId(string? cityName, int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId && c.Name == cityName);
        }
    }
}
