namespace EventManagement.UI.Models.DTOs
{
    public class EventFilterDto
    {
        public string? SearchTerm { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public bool? IsActive { get; set; }
        public string? Status { get; set; }
    }
} 