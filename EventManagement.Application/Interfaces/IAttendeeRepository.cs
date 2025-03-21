using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventManagement.Domain.Entities;
using EventManagement.Application.DTOs;

namespace EventManagement.Application.Interfaces
{
    /// <summary>
    /// Etkinlik katılımcıları için repository arayüzü
    /// </summary>
    public interface IAttendeeRepository
    {
        /// <summary>
        /// Etkinliğe ait tüm katılımcıları getirir
        /// </summary>
        Task<List<Attendee>> GetAttendeesByEventIdAsync(Guid eventId);
        
        /// <summary>
        /// ID'ye göre katılımcı bilgisini getirir
        /// </summary>
        Task<Attendee> GetAttendeeByIdAsync(Guid id);
        
        /// <summary>
        /// Yeni katılımcı oluşturur
        /// </summary>
        Task<Attendee> CreateAttendeeAsync(Attendee attendee);
        
        /// <summary>
        /// Mevcut katılımcı bilgisini günceller
        /// </summary>
        Task<Attendee> UpdateAttendeeAsync(Attendee attendee);
        
        /// <summary>
        /// Katılımcıyı soft-delete yapar
        /// </summary>
        Task<bool> DeleteAttendeeAsync(Guid id);
        
        /// <summary>
        /// E-posta adresine göre katılımcı arar
        /// </summary>
        Task<Attendee> SearchAttendeeByEmailAsync(string email);
        
        /// <summary>
        /// Belirtilen kriterlere göre katılımcı arar
        /// </summary>
        Task<List<Attendee>> SearchAttendeesAsync(Guid eventId, string searchTerm = null, int? status = null);
        
        /// <summary>
        /// Etkinlik bilgisini ID'ye göre getirir
        /// </summary>
        Task<Event> GetEventByIdAsync(Guid eventId);
        
        /// <summary>
        /// Etkinliğe ait onaylanmış katılımcı sayısını hesaplar
        /// </summary>
        Task<int> GetConfirmedAttendeesCountAsync(Guid eventId);
        
        /// <summary>
        /// Etkinlik istatistiklerini getirir
        /// </summary>
        Task<EventStatisticsDto> GetEventStatisticsAsync(Guid eventId);
        
        /// <summary>
        /// Değişiklikleri veritabanına kaydeder
        /// </summary>
        Task<int> SaveChangesAsync();
    }
} 