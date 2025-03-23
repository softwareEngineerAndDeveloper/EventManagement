using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventManagement.Application.DTOs;

namespace EventManagement.Application.Interfaces
{
    /// <summary>
    /// Etkinlik katılımcıları için servis arayüzü
    /// </summary>
    public interface IAttendeeService
    {
        /// <summary>
        /// Etkinliğe ait tüm katılımcıları getirir
        /// </summary>
        Task<ResponseDto<List<AttendeeDto>>> GetAttendeesByEventIdAsync(Guid eventId, Guid tenantId, bool isAdmin = false);
        
        /// <summary>
        /// ID'ye göre katılımcı bilgisini getirir
        /// </summary>
        Task<ResponseDto<AttendeeDto>> GetAttendeeByIdAsync(Guid id, Guid tenantId);
        
        /// <summary>
        /// Yeni katılımcı oluşturur
        /// </summary>
        Task<ResponseDto<AttendeeDto>> CreateAttendeeAsync(CreateAttendeeDto attendeeDto, Guid tenantId);
        
        /// <summary>
        /// Mevcut katılımcı bilgisini günceller
        /// </summary>
        Task<ResponseDto<AttendeeDto>> UpdateAttendeeAsync(Guid id, UpdateAttendeeDto attendeeDto, Guid tenantId);
        
        /// <summary>
        /// Katılımcıyı soft-delete yapar
        /// </summary>
        Task<ResponseDto<bool>> DeleteAttendeeAsync(Guid id, Guid tenantId);
        
        /// <summary>
        /// E-posta adresine göre katılımcı arar
        /// </summary>
        Task<ResponseDto<AttendeeDto>> SearchAttendeeByEmailAsync(string email, Guid tenantId);
        
        /// <summary>
        /// Belirtilen kriterlere göre katılımcı arar
        /// </summary>
        Task<ResponseDto<List<AttendeeDto>>> SearchAttendeesAsync(SearchAttendeeDto searchCriteria, Guid tenantId);
        
        /// <summary>
        /// Etkinlik istatistiklerini getirir
        /// </summary>
        Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid eventId, Guid tenantId, bool isAdmin = false);
    }
} 