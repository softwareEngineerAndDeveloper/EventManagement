using EventManagement.UI.Models.DTOs;

namespace EventManagement.UI.Models
{
    public class RoleViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    
    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<RoleViewModel>? Roles { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    
    public class AssignRoleDto
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
    
    public class UpdateUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
    
    public class EventsViewModel
    {
        public List<EventDto> Events { get; set; } = new List<EventDto>();
        public List<EventDto> PendingEvents { get; set; } = new List<EventDto>();
    }
} 