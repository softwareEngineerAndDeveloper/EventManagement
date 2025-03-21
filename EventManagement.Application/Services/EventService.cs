using EventManagement.Application.DTOs;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using System.Text.Json;

namespace EventManagement.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        public EventService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }
        
        public async Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(Guid tenantId)
        {
            string cacheKey = $"tenant:{tenantId}:events:list";
            return await _cacheService.GetOrSetAsync(cacheKey, 
                async () => {
                    var events = await _unitOfWork.Events.FindAsync(e => e.TenantId == tenantId);
                    var eventDtos = events.Select(e => MapToDto(e)).ToList();
                    return ResponseDto<List<EventDto>>.Success(eventDtos);
                }, 
                TimeSpan.FromMinutes(10));
        }
        
        public async Task<ResponseDto<EventDto>> GetEventByIdAsync(Guid id, Guid tenantId)
        {
            string cacheKey = $"tenant:{tenantId}:event:{id}";
            return await _cacheService.GetOrSetAsync(cacheKey, 
                async () => {
                    var events = await _unitOfWork.Events.FindAsync(e => e.Id == id && e.TenantId == tenantId);
                    var @event = events.FirstOrDefault();
                    
                    if (@event == null)
                        throw new NotFoundException(nameof(Event), id);
                        
                    return ResponseDto<EventDto>.Success(MapToDto(@event));
                }, 
                TimeSpan.FromMinutes(15));
        }
        
        public async Task<ResponseDto<List<EventDto>>> GetUpcomingEventsAsync(Guid tenantId)
        {
            string cacheKey = $"tenant:{tenantId}:events:upcoming";
            return await _cacheService.GetOrSetAsync(cacheKey, 
                async () => {
                    var events = await _unitOfWork.Events.FindAsync(e => 
                        e.TenantId == tenantId && 
                        e.StartDate > DateTime.UtcNow &&
                        !e.IsCancelled);
                        
                    var eventDtos = events.Select(e => MapToDto(e)).ToList();
                    
                    return ResponseDto<List<EventDto>>.Success(eventDtos);
                }, 
                TimeSpan.FromMinutes(5));
        }
        
        public async Task<ResponseDto<EventDto>> CreateEventAsync(CreateEventDto createEventDto, Guid tenantId)
        {
            var @event = new Event
            {
                Title = createEventDto.Title,
                Description = createEventDto.Description,
                StartDate = createEventDto.StartDate,
                EndDate = createEventDto.EndDate,
                Location = createEventDto.Location,
                MaxAttendees = createEventDto.MaxAttendees,
                IsPublic = createEventDto.IsPublic,
                TenantId = tenantId,
                Status = EventStatus.Approved,
                IsCancelled = false,
                CreatorId = createEventDto.CreatorId
            };
            
            await _unitOfWork.Events.AddAsync(@event);
            await _unitOfWork.SaveChangesAsync();
            
            // Kullanıcıyı etkinliğe attendee olarak ekleyelim
            var registration = new Registration
            {
                EventId = @event.Id,
                UserId = createEventDto.CreatorId,
                RegistrationDate = DateTime.UtcNow,
                Status = RegistrationStatus.Confirmed,
                HasAttended = false,
                IsCancelled = false
            };
            
            await _unitOfWork.Registrations.AddAsync(registration);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<EventDto>.Success(MapToDto(@event));
        }
        
        public async Task<ResponseDto<EventDto>> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, Guid tenantId)
        {
            var events = await _unitOfWork.Events.FindAsync(e => e.Id == id && e.TenantId == tenantId);
            var @event = events.FirstOrDefault();
            
            if (@event == null)
                throw new NotFoundException(nameof(Event), id);
                
            @event.Title = updateEventDto.Title;
            @event.Description = updateEventDto.Description;
            @event.StartDate = updateEventDto.StartDate;
            @event.EndDate = updateEventDto.EndDate;
            @event.Location = updateEventDto.Location;
            @event.MaxAttendees = updateEventDto.MaxAttendees;
            @event.Capacity = updateEventDto.MaxAttendees;
            @event.IsPublic = updateEventDto.IsPublic;
            @event.IsCancelled = updateEventDto.IsCancelled;
            @event.Status = updateEventDto.Status;
            
            await _unitOfWork.Events.UpdateAsync(@event);
            await _unitOfWork.SaveChangesAsync();
            
            // Önbellek invalidasyonu
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:list");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:upcoming");
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}:stats");
            
            return ResponseDto<EventDto>.Success(MapToDto(@event));
        }
        
        public async Task<ResponseDto<bool>> DeleteEventAsync(Guid id, Guid tenantId)
        {
            var events = await _unitOfWork.Events.FindAsync(e => e.Id == id && e.TenantId == tenantId);
            var @event = events.FirstOrDefault();
            
            if (@event == null)
                throw new NotFoundException(nameof(Event), id);
                
            await _unitOfWork.Events.DeleteAsync(@event);
            await _unitOfWork.SaveChangesAsync();
            
            // Önbellek invalidasyonu
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:list");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:upcoming");
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}:stats");
            
            return ResponseDto<bool>.Success(true);
        }
        
        public async Task<ResponseDto<bool>> CancelEventAsync(Guid id, Guid tenantId)
        {
            var events = await _unitOfWork.Events.FindAsync(e => e.Id == id && e.TenantId == tenantId);
            var @event = events.FirstOrDefault();
            
            if (@event == null)
                throw new NotFoundException(nameof(Event), id);
                
            @event.IsCancelled = true;
            @event.Status = Domain.Entities.EventStatus.Cancelled;
            await _unitOfWork.Events.UpdateAsync(@event);
            await _unitOfWork.SaveChangesAsync();
            
            // Önbellek invalidasyonu
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:list");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:upcoming");
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}:stats");
            
            return ResponseDto<bool>.Success(true);
        }
        
        public async Task<ResponseDto<bool>> ApproveEventAsync(Guid id, Guid tenantId)
        {
            var events = await _unitOfWork.Events.FindAsync(e => e.Id == id && e.TenantId == tenantId);
            var @event = events.FirstOrDefault();
            
            if (@event == null)
                throw new NotFoundException(nameof(Event), id);
                
            @event.Status = Domain.Entities.EventStatus.Approved;
            await _unitOfWork.Events.UpdateAsync(@event);
            await _unitOfWork.SaveChangesAsync();
            
            // Önbellek invalidasyonu
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:list");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:upcoming");
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}:stats");
            
            return ResponseDto<bool>.Success(true);
        }
        
        public async Task<ResponseDto<bool>> RejectEventAsync(Guid id, Guid tenantId)
        {
            var events = await _unitOfWork.Events.FindAsync(e => e.Id == id && e.TenantId == tenantId);
            var @event = events.FirstOrDefault();
            
            if (@event == null)
                throw new NotFoundException(nameof(Event), id);
                
            @event.Status = Domain.Entities.EventStatus.Rejected;
            await _unitOfWork.Events.UpdateAsync(@event);
            await _unitOfWork.SaveChangesAsync();
            
            // Önbellek invalidasyonu
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:list");
            await _cacheService.RemoveByPrefixAsync($"tenant:{tenantId}:events:upcoming");
            await _cacheService.RemoveAsync($"tenant:{tenantId}:event:{id}:stats");
            
            return ResponseDto<bool>.Success(true);
        }
        
        public async Task<ResponseDto<List<EventDto>>> GetPendingEventsAsync(Guid tenantId)
        {
            string cacheKey = $"tenant:{tenantId}:events:pending";
            return await _cacheService.GetOrSetAsync(cacheKey, 
                async () => {
                    var events = await _unitOfWork.Events.FindAsync(e => 
                        e.TenantId == tenantId && 
                        e.Status == Domain.Entities.EventStatus.Pending);
                        
                    var eventDtos = events.Select(e => MapToDto(e)).ToList();
                    
                    return ResponseDto<List<EventDto>>.Success(eventDtos);
                }, 
                TimeSpan.FromMinutes(5));
        }
        
        public async Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid id, Guid tenantId)
        {
            string cacheKey = $"tenant:{tenantId}:event:{id}:stats";
            return await _cacheService.GetOrSetAsync(cacheKey, 
                async () => {
                    var events = await _unitOfWork.Events.FindAsync(e => e.Id == id && e.TenantId == tenantId);
                    var @event = events.FirstOrDefault();
                    
                    if (@event == null)
                        throw new NotFoundException(nameof(Event), id);
                        
                    var registrations = await _unitOfWork.Registrations.FindAsync(r => r.EventId == id);
                    
                    var statistics = new EventStatisticsDto
                    {
                        EventId = id,
                        EventTitle = @event.Title,
                        Capacity = @event.Capacity ?? 0,
                        TotalRegistrations = registrations.Count(),
                        ConfirmedRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
                        CancelledRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Cancelled),
                        WaitingRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Waiting),
                        AvailableSeats = Math.Max(0, (@event.Capacity ?? 0) - registrations.Count(r => r.Status == RegistrationStatus.Confirmed)),
                        AttendedCount = registrations.Count(r => r.HasAttended),
                        AttendanceRate = registrations.Any() 
                            ? (decimal)registrations.Count(r => r.HasAttended) / registrations.Count() * 100
                            : 0
                    };
                    
                    return ResponseDto<EventStatisticsDto>.Success(statistics);
                }, 
                TimeSpan.FromMinutes(5));
        }
        
        private EventDto MapToDto(Event @event)
        {
            return new EventDto
            {
                Id = @event.Id,
                Title = @event.Title,
                Description = @event.Description,
                StartDate = @event.StartDate,
                EndDate = @event.EndDate,
                Location = @event.Location,
                MaxAttendees = @event.MaxAttendees,
                IsPublic = @event.IsPublic,
                IsCancelled = @event.IsCancelled,
                Status = @event.Status,
                TenantId = @event.TenantId,
                CreatorId = @event.CreatorId,
                CreatedDate = @event.CreatedDate,
                UpdatedDate = @event.UpdatedDate
            };
        }
    }
} 