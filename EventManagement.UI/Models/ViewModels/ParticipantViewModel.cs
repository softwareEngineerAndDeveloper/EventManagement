using System;
using System.Collections.Generic;
using EventManagement.Application.DTOs;

namespace EventManagement.UI.Models.ViewModels
{
    /// <summary>
    /// Katılımcı ekranları için kullanılacak view model
    /// </summary>
    public class ParticipantViewModel
    {
        /// <summary>
        /// Etkinlik ID
        /// </summary>
        public Guid EventId { get; set; }
        
        /// <summary>
        /// Etkinlik başlığı
        /// </summary>
        public string EventTitle { get; set; }
        
        /// <summary>
        /// Katılımcı listesi
        /// </summary>
        public List<ParticipantItemViewModel> Attendees { get; set; } = new List<ParticipantItemViewModel>();
        
        /// <summary>
        /// Etkinlik istatistikleri
        /// </summary>
        public EventStatisticsViewModel Statistics { get; set; }
    }
    
    /// <summary>
    /// Katılımcı liste öğesi için view model
    /// </summary>
    public class ParticipantItemViewModel
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Status { get; set; }
        public bool HasAttended { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Notes { get; set; }
        public bool IsCancelled { get; set; }
    }
    
    /// <summary>
    /// Etkinlik istatistikleri için view model
    /// </summary>
    public class EventStatisticsViewModel
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; }
        public int TotalAttendees { get; set; }
        public int ConfirmedAttendees { get; set; }
        public int CancelledAttendees { get; set; }
        public int WaitingListAttendees { get; set; }
        public int AvailableCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public double FillRate { get; set; }
    }
} 