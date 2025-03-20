using EventManagement.Application.DTOs;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EventManagement.Application.Helpers;

namespace EventManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ICacheService _cacheService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public UserService(IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenGenerator = jwtTokenGenerator;
            _cacheService = cacheService;
        }

        public async Task<ResponseDto<List<UserDto>>> GetAllUsersAsync(Guid tenantId)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.TenantId == tenantId);
            var userDtos = users.Select(u => MapToDto(u)).ToList();
            return ResponseDto<List<UserDto>>.Success(userDtos);
        }

        public async Task<ResponseDto<UserDto>> GetUserByIdAsync(Guid id, Guid tenantId)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.Id == id && u.TenantId == tenantId);
            var userFromDb = user.FirstOrDefault();
            
            if (userFromDb == null)
                throw new NotFoundException(nameof(User), id);

            return ResponseDto<UserDto>.Success(MapToDto(userFromDb));
        }

        public async Task<ResponseDto<UserDto>> GetUserByEmailAsync(string email, Guid tenantId)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Email == email && u.TenantId == tenantId);
            var user = users.FirstOrDefault();
            
            if (user == null)
                throw new NotFoundException(nameof(User), email);

            return ResponseDto<UserDto>.Success(MapToDto(user));
        }

        public async Task<ResponseDto<UserDto>> CreateUserAsync(CreateUserDto createUserDto, Guid tenantId)
        {
            var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Email == createUserDto.Email && u.TenantId == tenantId);
            if (existingUsers.Any())
                throw new ValidationException($"Email {createUserDto.Email} zaten kullanımda.");

            var user = new User
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.PhoneNumber,
                PasswordHash = HashPassword(createUserDto.Password),
                TenantId = tenantId,
                IsActive = true
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDto<UserDto>.Success(MapToDto(user));
        }

        public async Task<ResponseDto<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto, Guid tenantId)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Id == id && u.TenantId == tenantId);
            var user = users.FirstOrDefault();
            
            if (user == null)
                throw new NotFoundException(nameof(User), id);

            user.FirstName = updateUserDto.FirstName;
            user.LastName = updateUserDto.LastName;
            user.PhoneNumber = updateUserDto.PhoneNumber;
            user.IsActive = updateUserDto.IsActive;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDto<UserDto>.Success(MapToDto(user));
        }

        public async Task<ResponseDto<bool>> DeleteUserAsync(Guid id, Guid tenantId)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Id == id && u.TenantId == tenantId);
            var user = users.FirstOrDefault();
            
            if (user == null)
                throw new NotFoundException(nameof(User), id);

            await _unitOfWork.Users.DeleteAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDto<bool>.Success(true);
        }

        public async Task<ResponseDto<string>> AuthenticateAsync(LoginDto loginDto)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Email == loginDto.Email);
            var user = users.FirstOrDefault();

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedException();

            var token = await _jwtTokenGenerator.GenerateTokenAsync(user);
            return ResponseDto<string>.Success(token);
        }

        public async Task<ResponseDto<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, Guid tenantId)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Id == userId && u.TenantId == tenantId);
            var user = users.FirstOrDefault();
            
            if (user == null)
                throw new NotFoundException(nameof(User), userId);

            if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                throw new ValidationException("Mevcut şifre yanlış.");

            user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDto<bool>.Success(true);
        }

        public async Task<ResponseDto<bool>> AssignRoleToUserAsync(AssignRoleDto assignRoleDto, Guid tenantId)
        {
            var userExists = await _unitOfWork.Users.FindAsync(u => u.Id == assignRoleDto.UserId && u.TenantId == tenantId);
            if (!userExists.Any())
                throw new NotFoundException(nameof(User), assignRoleDto.UserId);

            var roleExists = await _unitOfWork.Roles.FindAsync(r => r.Id == assignRoleDto.RoleId && r.TenantId == tenantId);
            if (!roleExists.Any())
                throw new NotFoundException(nameof(Role), assignRoleDto.RoleId);

            var userRoles = await _unitOfWork.UserRoles.FindAsync(ur => ur.UserId == assignRoleDto.UserId && ur.RoleId == assignRoleDto.RoleId);
            if (userRoles.Any())
                return ResponseDto<bool>.Success(true); // Rol zaten atanmış
            
            var userRole = new UserRole
            {
                UserId = assignRoleDto.UserId,
                RoleId = assignRoleDto.RoleId,
                TenantId = tenantId
            };
            
            await _unitOfWork.UserRoles.AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<bool>.Success(true);
        }
        
        public async Task<ResponseDto<List<RoleDto>>> GetRolesAsync(Guid tenantId)
        {
            var roles = await _unitOfWork.Roles.FindAsync(r => r.TenantId == tenantId);
            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            }).ToList();
            
            return ResponseDto<List<RoleDto>>.Success(roleDtos);
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                TenantId = user.TenantId,
                Roles = user.UserRoles?.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description
                }).ToList(),
                CreatedDate = user.CreatedDate,
                UpdatedDate = user.UpdatedDate
            };
        }

        private string HashPassword(string password)
        {
            return PasswordHelper.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return PasswordHelper.VerifyPassword(password, hash);
        }

        private byte[] Serialize<T>(T obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj, _jsonOptions);
        }
        
        private T Deserialize<T>(byte[] bytes)
        {
            if (bytes == null)
                return default!;
                
            return JsonSerializer.Deserialize<T>(bytes, _jsonOptions) ?? default!;
        }
    }

    public interface ICacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
    }
} 