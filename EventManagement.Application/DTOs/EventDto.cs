using System;
using System.ComponentModel.DataAnnotations;
using EventManagement.Domain.Entities;

namespace EventManagement.Application.DTOs
{
    public class EventDto
    {
        public Guid Id { get; set; }
        
        public required string Title { get; set; }
        
        public required string Description { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public required string Location { get; set; }
        
        public int? MaxAttendees { get; set; }
        
        public bool IsPublic { get; set; }
        
        public bool IsCancelled { get; set; }
        
        public EventStatus Status { get; set; }
        
        public Guid TenantId { get; set; }
        
        public Guid CreatorId { get; set; }
        
        public int RegistrationCount { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateEventDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;
        
        public int? MaxAttendees { get; set; }
        
        public bool IsPublic { get; set; } = true;
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public Guid CreatorId { get; set; }
    }

    public class UpdateEventDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;
        
        public int? MaxAttendees { get; set; }
        
        public bool IsPublic { get; set; }
        
        public bool IsCancelled { get; set; }
        
        public EventStatus Status { get; set; } = EventStatus.Approved;
    }

    public class EventFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsUpcoming { get; set; }
        public bool? IsPast { get; set; }
        public bool? IsCancelled { get; set; }
    }
} 