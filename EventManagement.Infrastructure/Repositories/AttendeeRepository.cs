using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Data;
using EventManagement.Application.DTOs;

namespace EventManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Etkinlik katılımcıları için repository sınıfı
    /// </summary>
    public class AttendeeRepository : IAttendeeRepository
    {
        private readonly ApplicationDbContext _dbContext;
        
        public AttendeeRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        
        /// <summary>
        /// Etkinliğe ait tüm katılımcıları getirir
        /// </summary>
        public async Task<List<Attendee>> GetAttendeesByEventIdAsync(Guid eventId)
        {
            return await _dbContext.Attendees
                .Where(a => a.EventId == eventId && !a.IsDeleted)
                .ToListAsync();
        }
        
        /// <summary>
        /// ID'ye göre katılımcı bilgisini getirir
        /// </summary>
        public async Task<Attendee> GetAttendeeByIdAsync(Guid id)
        {
            return await _dbContext.Attendees
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }
        
        /// <summary>
        /// Yeni katılımcı oluşturur
        /// </summary>
        public async Task<Attendee> CreateAttendeeAsync(Attendee attendee)
        {
            await _dbContext.Attendees.AddAsync(attendee);
            await SaveChangesAsync();
            return attendee;
        }
        
        /// <summary>
        /// Mevcut katılımcı bilgisini günceller
        /// </summary>
        public async Task<Attendee> UpdateAttendeeAsync(Attendee attendee)
        {
            _dbContext.Attendees.Update(attendee);
            await SaveChangesAsync();
            return attendee;
        }
        
        /// <summary>
        /// Katılımcıyı soft-delete yapar
        /// </summary>
        public async Task<bool> DeleteAttendeeAsync(Guid id)
        {
            var attendee = await GetAttendeeByIdAsync(id);
            if (attendee == null)
            {
                return false;
            }
            
            attendee.IsDeleted = true;
            
            _dbContext.Attendees.Update(attendee);
            await SaveChangesAsync();
            return true;
        }
        
        /// <summary>
        /// E-posta adresine göre katılımcı arar
        /// </summary>
        public async Task<Attendee> SearchAttendeeByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }
            
            return await _dbContext.Attendees
                .FirstOrDefaultAsync(a => a.Email == email && !a.IsDeleted);
        }
        
        /// <summary>
        /// Belirtilen kriterlere göre katılımcı arar
        /// </summary>
        public async Task<List<Attendee>> SearchAttendeesAsync(Guid eventId, string searchTerm = null, int? status = null)
        {
            var query = _dbContext.Attendees
                .Where(a => a.EventId == eventId && !a.IsDeleted);
            
            // Arama terimi varsa filtrele
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(a => 
                    a.Name.ToLower().Contains(searchTerm) || 
                    a.Email.ToLower().Contains(searchTerm) ||
                    (a.Phone != null && a.Phone.Contains(searchTerm)) ||
                    (a.Notes != null && a.Notes.ToLower().Contains(searchTerm))
                );
            }
            
            // Durum filtresi
            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }
            
            return await query.ToListAsync();
        }
        
        /// <summary>
        /// Etkinlik bilgisini ID'ye göre getirir
        /// </summary>
        public async Task<Event> GetEventByIdAsync(Guid eventId)
        {
            return await _dbContext.Events
                .FirstOrDefaultAsync(e => e.Id == eventId && !e.IsDeleted);
        }
        
        /// <summary>
        /// Etkinliğe ait onaylanmış katılımcı sayısını hesaplar
        /// </summary>
        public async Task<int> GetConfirmedAttendeesCountAsync(Guid eventId)
        {
            return await _dbContext.Attendees
                .CountAsync(a => a.EventId == eventId && a.Status == 1 && !a.IsCancelled && !a.IsDeleted);
        }
        
        /// <summary>
        /// Etkinlik istatistiklerini getirir
        /// </summary>
        public async Task<EventStatisticsDto> GetEventStatisticsAsync(Guid eventId)
        {
            // Etkinlik bilgisini al
            var eventEntity = await GetEventByIdAsync(eventId);
            if (eventEntity == null)
            {
                throw new Exception($"Etkinlik bulunamadı. EventId: {eventId}");
            }
            
            // Etkinliğe ait tüm katılımcıları getir
            var attendees = await GetAttendeesByEventIdAsync(eventId);
            
            // İstatistikleri hesapla
            var confirmedAttendees = attendees.Count(a => a.Status == 1 && !a.IsCancelled);
            var cancelledAttendees = attendees.Count(a => a.Status == 2 || a.IsCancelled);
            var waitingListAttendees = attendees.Count(a => a.Status == 0);
            var attendedCount = attendees.Count(a => a.HasAttended);
            
            var maxCapacity = eventEntity.MaxAttendees;
            var availableCapacity = maxCapacity.HasValue 
                ? Math.Max(0, maxCapacity.Value - confirmedAttendees) 
                : 0;
            
            var fillRate = maxCapacity.HasValue && maxCapacity.Value > 0 
                ? (double)confirmedAttendees / maxCapacity.Value * 100 
                : 0;
            
            var attendanceRate = attendees.Any() && confirmedAttendees > 0
                ? (decimal)attendedCount / confirmedAttendees * 100
                : 0;
            
            // DTO oluştur ve doldur
            return new EventStatisticsDto
            {
                EventId = eventId,
                EventTitle = eventEntity.Title,
                TotalAttendees = attendees.Count,
                ConfirmedAttendees = confirmedAttendees,
                CancelledAttendees = cancelledAttendees,
                WaitingListAttendees = waitingListAttendees,
                AvailableCapacity = availableCapacity,
                MaxCapacity = maxCapacity,
                Capacity = eventEntity.Capacity ?? 0,
                TotalRegistrations = attendees.Count,
                ConfirmedRegistrations = confirmedAttendees,
                CancelledRegistrations = cancelledAttendees,
                WaitingRegistrations = waitingListAttendees,
                AvailableSeats = availableCapacity,
                AttendedCount = attendedCount,
                AttendanceRate = attendanceRate,
                FillRate = fillRate
            };
        }
        
        /// <summary>
        /// Değişiklikleri veritabanına kaydeder
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
} 