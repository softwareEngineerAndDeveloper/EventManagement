using System;
using System.Collections.Generic;
using EventManagement.UI.DTOs;

namespace EventManagement.UI.Models
{
    /// <summary>
    /// Etkinlik katılımcıları sayfası için view model
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
        public string EventTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// Katılımcı listesi
        /// </summary>
        public List<ParticipantItemViewModel> Attendees { get; set; } = new List<ParticipantItemViewModel>();
        
        /// <summary>
        /// Etkinlik istatistikleri
        /// </summary>
        public EventStatisticsViewModel Statistics { get; set; } = new EventStatisticsViewModel();
    }
    
    /// <summary>
    /// Katılımcı listesi öğesi için view model
    /// </summary>
    public class ParticipantItemViewModel
    {
        /// <summary>
        /// Katılımcı ID
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Etkinlik ID
        /// </summary>
        public Guid EventId { get; set; }
        
        /// <summary>
        /// Katılımcı adı
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// E-posta adresi
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Telefon numarası
        /// </summary>
        public string Phone { get; set; } = string.Empty;
        
        /// <summary>
        /// Katılımcı durumu (0: Beklemede, 1: Onaylandı, 2: İptal edildi)
        /// </summary>
        public int Status { get; set; }
        
        /// <summary>
        /// Etkinliğe katılıp katılmadığı
        /// </summary>
        public bool HasAttended { get; set; }
        
        /// <summary>
        /// Kayıt tarihi
        /// </summary>
        public DateTime RegistrationDate { get; set; }
        
        /// <summary>
        /// Katılımcı hakkında notlar
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// Kayıt iptal edildi mi
        /// </summary>
        public bool IsCancelled { get; set; }
    }
    
    /// <summary>
    /// Etkinlik istatistikleri için view model
    /// </summary>
    public class EventStatisticsViewModel
    {
        /// <summary>
        /// Etkinlik ID
        /// </summary>
        public Guid EventId { get; set; }
        
        /// <summary>
        /// Etkinlik başlığı
        /// </summary>
        public string EventTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// Toplam katılımcı sayısı
        /// </summary>
        public int TotalAttendees { get; set; }
        
        /// <summary>
        /// Onaylanmış katılımcı sayısı
        /// </summary>
        public int ConfirmedAttendees { get; set; }
        
        /// <summary>
        /// İptal edilmiş katılımcı sayısı
        /// </summary>
        public int CancelledAttendees { get; set; }
        
        /// <summary>
        /// Bekleme listesindeki katılımcı sayısı
        /// </summary>
        public int WaitingListAttendees { get; set; }
        
        /// <summary>
        /// Mevcut kapasite
        /// </summary>
        public int AvailableCapacity { get; set; }
        
        /// <summary>
        /// Maksimum kapasite (null olabilir)
        /// </summary>
        public int? MaxCapacity { get; set; }
        
        /// <summary>
        /// Doluluk oranı (%)
        /// </summary>
        public double FillRate { get; set; }
    }
} 