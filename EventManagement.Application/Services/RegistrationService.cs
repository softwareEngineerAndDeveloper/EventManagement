using EventManagement.Application.DTOs;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;

namespace EventManagement.Application.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public RegistrationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        public async Task<ResponseDto<List<RegistrationDto>>> GetAllRegistrationsAsync(Guid tenantId)
        {
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.Event.TenantId == tenantId);
            var registrationDtos = registrations.Select(r => MapToDto(r)).ToList();
            
            return ResponseDto<List<RegistrationDto>>.Success(registrationDtos);
        }
        
        public async Task<ResponseDto<RegistrationDto>> GetRegistrationByIdAsync(Guid id, Guid tenantId)
        {
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.Id == id && r.Event.TenantId == tenantId);
            var registration = registrations.FirstOrDefault();
            
            if (registration == null)
                throw new NotFoundException(nameof(Registration), id);
                
            return ResponseDto<RegistrationDto>.Success(MapToDto(registration));
        }
        
        public async Task<ResponseDto<List<RegistrationDto>>> GetRegistrationsByEventIdAsync(Guid eventId, Guid tenantId)
        {
            var events = await _unitOfWork.Events.FindAsync(e => e.Id == eventId && e.TenantId == tenantId);
            var @event = events.FirstOrDefault();
            
            if (@event == null)
                throw new NotFoundException(nameof(Event), eventId);
                
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.EventId == eventId);
            var registrationDtos = registrations.Select(r => MapToDto(r)).ToList();
            
            return ResponseDto<List<RegistrationDto>>.Success(registrationDtos);
        }
        
        public async Task<ResponseDto<List<RegistrationDto>>> GetRegistrationsByUserIdAsync(Guid userId, Guid tenantId)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Id == userId && u.TenantId == tenantId);
            var user = users.FirstOrDefault();
            
            if (user == null)
                throw new NotFoundException(nameof(User), userId);
                
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.UserId == userId && r.Event.TenantId == tenantId);
            var registrationDtos = registrations.Select(r => MapToDto(r)).ToList();
            
            return ResponseDto<List<RegistrationDto>>.Success(registrationDtos);
        }
        
        public async Task<ResponseDto<RegistrationDto>> CreateRegistrationAsync(CreateRegistrationDto createRegistrationDto, Guid tenantId, Guid currentUserId)
        {
            var events = await _unitOfWork.Events.FindAsync(e => e.Id == createRegistrationDto.EventId && e.TenantId == tenantId);
            var @event = events.FirstOrDefault();
            
            if (@event == null)
                throw new NotFoundException(nameof(Event), createRegistrationDto.EventId);
                
            if (@event.IsCancelled)
                throw new ValidationException("Bu etkinlik iptal edildi, kayıt yapılamaz.");
                
            if (@event.StartDate < DateTime.UtcNow)
                throw new ValidationException("Bu etkinlik için kayıt süresi sona erdi.");
                
            // Etkinliğin maksimum katılımcı sayısını kontrol et
            if (@event.MaxAttendees.HasValue)
            {
                var currentRegistrations = await _unitOfWork.Registrations.FindAsync(r => r.EventId == @event.Id && !r.IsCancelled);
                if (currentRegistrations.Count() >= @event.MaxAttendees.Value)
                    throw new ValidationException("Bu etkinlik için maksimum katılımcı sayısına ulaşıldı.");
            }
            
            Guid userId = currentUserId;
            
            // Anonim kayıt için (kullanıcı girişi yapılmadan)
            if (userId == Guid.Empty)
            {
                if (string.IsNullOrEmpty(createRegistrationDto.Email))
                    throw new ValidationException("Kayıt için e-posta adresi gereklidir.");
                    
                // E-posta ile kullanıcı kontrolü
                var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Email == createRegistrationDto.Email && u.TenantId == tenantId);
                var existingUser = existingUsers.FirstOrDefault();
                
                if (existingUser != null)
                {
                    userId = existingUser.Id;
                }
                else
                {
                    // Yeni kullanıcı oluştur
                    var newUser = new User
                    {
                        FirstName = createRegistrationDto.FirstName ?? "Misafir",
                        LastName = createRegistrationDto.LastName ?? "Kullanıcı",
                        Email = createRegistrationDto.Email ?? "misafir@etkinlik.com",
                        PhoneNumber = createRegistrationDto.PhoneNumber ?? "",
                        TenantId = tenantId,
                        IsActive = true,
                        PasswordHash = "temp" // Gerçek uygulamada bu şekilde yapılmamalı
                    };
                    
                    await _unitOfWork.Users.AddAsync(newUser);
                    await _unitOfWork.SaveChangesAsync();
                    
                    userId = newUser.Id;
                }
            }
            
            // Kullanıcının bu etkinliğe daha önce kayıt olup olmadığını kontrol et
            var userRegistrations = await _unitOfWork.Registrations.FindAsync(r => r.UserId == userId && r.EventId == @event.Id && !r.IsCancelled);
            if (userRegistrations.Any())
                throw new ValidationException("Bu etkinliğe zaten kaydoldunuz.");
                
            var registration = new Registration
            {
                EventId = @event.Id,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow,
                AdditionalNotes = createRegistrationDto.AdditionalNotes,
                IsCancelled = false,
                HasAttended = false,
                Status = RegistrationStatus.Waiting
            };
            
            await _unitOfWork.Registrations.AddAsync(registration);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<RegistrationDto>.Success(MapToDto(registration));
        }
        
        public async Task<ResponseDto<RegistrationDto>> UpdateRegistrationAsync(Guid id, UpdateRegistrationDto updateRegistrationDto, Guid tenantId)
        {
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.Id == id && r.Event.TenantId == tenantId);
            var registration = registrations.FirstOrDefault();
            
            if (registration == null)
                throw new NotFoundException(nameof(Registration), id);
                
            registration.AdditionalNotes = updateRegistrationDto.AdditionalNotes;
            
            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<RegistrationDto>.Success(MapToDto(registration));
        }
        
        public async Task<ResponseDto<bool>> DeleteRegistrationAsync(Guid id, Guid tenantId)
        {
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.Id == id && r.Event.TenantId == tenantId);
            var registration = registrations.FirstOrDefault();
            
            if (registration == null)
                throw new NotFoundException(nameof(Registration), id);
                
            await _unitOfWork.Registrations.DeleteAsync(registration);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<bool>.Success(true);
        }
        
        public async Task<ResponseDto<bool>> CancelRegistrationAsync(Guid id, Guid tenantId, Guid currentUserId)
        {
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.Id == id && r.Event.TenantId == tenantId);
            var registration = registrations.FirstOrDefault();
            
            if (registration == null)
                throw new NotFoundException(nameof(Registration), id);
                
            // Sadece kendi kaydını iptal edebilir veya tenant admin
            if (registration.UserId != currentUserId)
            {
                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                if (currentUser == null || currentUser.TenantId != tenantId)
                    throw new UnauthorizedException();
            }
            
            registration.IsCancelled = true;
            
            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<bool>.Success(true);
        }
        
        public async Task<ResponseDto<bool>> MarkAttendanceAsync(Guid id, bool hasAttended, Guid tenantId)
        {
            var registrations = await _unitOfWork.Registrations.FindAsync(r => r.Id == id && r.Event.TenantId == tenantId);
            var registration = registrations.FirstOrDefault();
            
            if (registration == null)
                throw new NotFoundException(nameof(Registration), id);
                
            registration.HasAttended = hasAttended;
            
            await _unitOfWork.Registrations.UpdateAsync(registration);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<bool>.Success(true);
        }
        
        // Dashboard istatistikleri için eklenen metod
        public async Task<int> GetTotalRegistrationsCountAsync(Guid? tenantId)
        {
            try
            {
                if (tenantId.HasValue)
                {
                    var registrations = await _unitOfWork.Registrations.FindAsync(r => 
                        r.Event.TenantId == tenantId.Value && 
                        !r.IsCancelled);
                    return registrations.Count();
                }
                else
                {
                    var registrations = await _unitOfWork.Registrations.FindAsync(r => !r.IsCancelled);
                    return registrations.Count();
                }
            }
            catch (Exception)
            {
                return 0; // Hata durumunda 0 döndür
            }
        }
        
        private RegistrationDto MapToDto(Registration registration)
        {
            return new RegistrationDto
            {
                Id = registration.Id,
                EventId = registration.EventId,
                UserId = registration.UserId,
                RegistrationDate = registration.RegistrationDate,
                AdditionalNotes = registration.AdditionalNotes,
                IsCancelled = registration.IsCancelled,
                HasAttended = registration.HasAttended,
                CreatedDate = registration.CreatedDate,
                UpdatedDate = registration.UpdatedDate
            };
        }
    }
} 