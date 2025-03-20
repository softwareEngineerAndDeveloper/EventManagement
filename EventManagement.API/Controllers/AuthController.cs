using EventManagement.API.Extensions;
using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IUserService _userService;
        
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }
        
        [HttpPost("login")]
        public async Task<ActionResult<ResponseDto<string>>> Login(LoginDto loginDto)
        {
            var result = await _userService.AuthenticateAsync(loginDto);
            return Ok(result);
        }
        
        [HttpPost("register")]
        public async Task<ActionResult<ResponseDto<UserDto>>> Register(CreateUserDto createUserDto)
        {
            var tenantId = GetTenantId();
            var result = await _userService.CreateUserAsync(createUserDto, tenantId);
            return Ok(result);
        }
        
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ResponseDto<bool>>> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();
            var result = await _userService.ChangePasswordAsync(userId, changePasswordDto, tenantId);
            return Ok(result);
        }
    }
} 