using System.ComponentModel.DataAnnotations;

namespace EventManagement.Application.DTOs
{
    public class RegistrationDto
    {
        public Guid Id { get; set; }
        
        public Guid EventId { get; set; }
        
        public Guid UserId { get; set; }
        
        public DateTime RegistrationDate { get; set; }
        
        public string? AdditionalNotes { get; set; }
        
        public bool IsCancelled { get; set; }
        
        public bool HasAttended { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    
    public class CreateRegistrationDto
    {
        [Required]
        public Guid EventId { get; set; }
        
        // Anonim kayıt için gerekli alanlar
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        
        public string? AdditionalNotes { get; set; }
    }
    
    public class UpdateRegistrationDto
    {
        public string? AdditionalNotes { get; set; }
    }
} 