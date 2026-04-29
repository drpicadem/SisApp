namespace ŠišAppApi.Models.DTOs
{
    public class MonthlyRegistrationDto
    {
        public int Month { get; set; }
        public int Count { get; set; }
    }

    public class StatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalBarbers { get; set; }
        public int TotalSalons { get; set; }
        public IEnumerable<MonthlyRegistrationDto> MonthlyRegistrations { get; set; } = new List<MonthlyRegistrationDto>();
    }
}
