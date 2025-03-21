using EventManagement.API.Extensions;
using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [Authorize]
    [Route("api/users")]
    public class UserController : BaseApiController
    {
        private readonly IUserService _userService;
        
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        
        [HttpGet]
        public async Task<ActionResult<ResponseDto<List<UserDto>>>> GetAllUsers()
        {
            var tenantId = GetTenantId();
            var result = await _userService.GetAllUsersAsync(tenantId);
            return Ok(result);
        }
        
        [HttpGet("me")]
        public async Task<ActionResult<ResponseDto<UserDto>>> GetCurrentUser()
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();
            var result = await _userService.GetUserByIdAsync(userId, tenantId);
            return Ok(result);
        }
        
        [HttpPut("me")]
        public async Task<ActionResult<ResponseDto<UserDto>>> UpdateCurrentUser(UpdateUserDto updateUserDto)
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();
            var result = await _userService.UpdateUserAsync(userId, updateUserDto, tenantId);
            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseDto<UserDto>>> GetUserById(Guid id)
        {
            var tenantId = GetTenantId();
            var result = await _userService.GetUserByIdAsync(id, tenantId);
            return Ok(result);
        }
        
        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseDto<UserDto>>> UpdateUser(Guid id, UpdateUserDto updateUserDto)
        {
            var tenantId = GetTenantId();
            var result = await _userService.UpdateUserAsync(id, updateUserDto, tenantId);
            return Ok(result);
        }
        
        [HttpDelete("{id}")]
        public async Task<ActionResult<ResponseDto<bool>>> DeleteUser(Guid id)
        {
            var tenantId = GetTenantId();
            var result = await _userService.DeleteUserAsync(id, tenantId);
            return Ok(result);
        }
        
        [HttpGet("{id}/roles")]
        public async Task<ActionResult<ResponseDto<List<Guid>>>> GetUserRoles(Guid id)
        {
            var tenantId = GetTenantId();
            var user = await _userService.GetUserByIdAsync(id, tenantId);
            
            if (!user.IsSuccess)
            {
                return NotFound(ResponseDto<List<Guid>>.Fail("Kullanıcı bulunamadı"));
            }
            
            var roles = user.Data.Roles?.Select(r => r.Id).ToList() ?? new List<Guid>();
            return Ok(ResponseDto<List<Guid>>.Success(roles));
        }
        
        [HttpPost("{id}/roles")]
        public async Task<ActionResult<ResponseDto<bool>>> UpdateUserRoles(Guid id, [FromBody] UserRoleUpdateRequest request)
        {
            var tenantId = GetTenantId();
            
            // Kullanıcı mevcut mu diye kontrol et
            var userResult = await _userService.GetUserByIdAsync(id, tenantId);
            if (!userResult.IsSuccess)
            {
                return NotFound(ResponseDto<bool>.Fail("Kullanıcı bulunamadı"));
            }
            
            // Her rol için ayrı işlem yap
            var results = new List<bool>();
            foreach (var roleId in request.RoleIds)
            {
                var assignRoleDto = new AssignRoleDto
                {
                    UserId = id,
                    RoleId = roleId
                };
                
                var result = await _userService.AssignRoleToUserAsync(assignRoleDto, tenantId);
                results.Add(result.IsSuccess);
            }
            
            return Ok(ResponseDto<bool>.Success(true, "Kullanıcı rolleri başarıyla güncellendi"));
        }
    }
    
    public class UserRoleUpdateRequest
    {
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
} 