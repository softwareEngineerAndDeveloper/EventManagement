namespace EventManagement.Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalUsers { get; set; }
    }
} 