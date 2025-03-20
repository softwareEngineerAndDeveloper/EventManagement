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
        public required string Title { get; set; }

        [MaxLength(500)]
        public required string Description { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [MaxLength(200)]
        public required string Location { get; set; }
        
        public int? MaxAttendees { get; set; }
        
        public bool IsPublic { get; set; } = true;
        
        [Required]
        public Guid CreatorId { get; set; }
    }
    
    public class UpdateEventDto
    {
        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }

        [MaxLength(500)]
        public required string Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [MaxLength(200)]
        public required string Location { get; set; }

        public int? MaxAttendees { get; set; }

        public bool IsPublic { get; set; }
        
        public bool IsCancelled { get; set; }
    }

    // Filtreleme için DTO
    public class EventFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsUpcoming { get; set; }
        public bool? IsPast { get; set; }
        public bool? IsCancelled { get; set; }
    }

    // İstatistikler için DTO
    public class EventStatisticsDto
    {
        public Guid EventId { get; set; }
        public required string EventTitle { get; set; }
        public int Capacity { get; set; }
        public int TotalRegistrations { get; set; }
        public int ConfirmedRegistrations { get; set; }
        public int CancelledRegistrations { get; set; }
        public int WaitingRegistrations { get; set; }
        public int AvailableSeats { get; set; }
        public int AttendedCount { get; set; }
        public decimal AttendanceRate { get; set; } // Katılım oranı (yüzde)
    }
} 