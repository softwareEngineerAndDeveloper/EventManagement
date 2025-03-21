namespace EventManagement.UI.Models
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public ApiEndpoints Endpoints { get; set; } = new ApiEndpoints();
        public string DefaultTenant { get; set; } = string.Empty;
    }

    public class ApiEndpoints
    {
        public string Auth { get; set; } = string.Empty;
        public string Events { get; set; } = string.Empty;
        public string Tenants { get; set; } = string.Empty;
        public string Users { get; set; } = string.Empty;
        public string Registrations { get; set; } = string.Empty;
        public string Attendees { get; set; } = string.Empty;
    }
} 