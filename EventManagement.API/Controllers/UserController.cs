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
    }
} 