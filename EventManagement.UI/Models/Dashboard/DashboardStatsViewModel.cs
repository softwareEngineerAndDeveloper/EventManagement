using System.Text.Json.Serialization;

namespace EventManagement.UI.Models.Dashboard
{
    public class DashboardStatsViewModel
    {
        [JsonPropertyName("totalEvents")]
        public int TotalEvents { get; set; }
        
        [JsonPropertyName("activeEvents")]
        public int ActiveEvents { get; set; }
        
        [JsonPropertyName("upcomingEvents")]
        public int UpcomingEvents { get; set; }
        
        [JsonPropertyName("totalRegistrations")]
        public int TotalRegistrations { get; set; }
        
        [JsonPropertyName("totalUsers")]
        public int TotalUsers { get; set; }
    }
} 