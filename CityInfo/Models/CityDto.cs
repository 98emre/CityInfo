namespace CityInfo.Models
{
    public class CityDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<PointsOfInterestDto> PointOfInterest { get; set; } = new List<PointsOfInterestDto>();

        public int NumberOfPoint { get { return PointOfInterest.Count(); } }
    }
}
