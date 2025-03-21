using EventManagement.API.Extensions;
using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventManagement.API.Controllers
{
    [Authorize]
    [Route("api/roles")]
    public class RoleController : BaseApiController
    {
        private readonly IUserService _userService;
        
        public RoleController(IUserService userService)
        {
            _userService = userService;
        }
        
        [HttpGet]
        public async Task<ActionResult<ResponseDto<List<RoleDto>>>> GetRoles()
        {
            var tenantId = GetTenantId();
            var result = await _userService.GetRolesAsync(tenantId);
            return Ok(result);
        }
        
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ResponseDto<List<Guid>>>> GetUserRoles(Guid userId)
        {
            var tenantId = GetTenantId();
            
            // Önce kullanıcının var olup olmadığını kontrol et
            var userCheck = await _userService.GetUserByIdAsync(userId, tenantId);
            if (!userCheck.IsSuccess)
            {
                return NotFound(ResponseDto<List<Guid>>.Fail("Kullanıcı bulunamadı"));
            }
            
            // Kullanıcının rollerini direkt olarak UserService'ten al
            var userRoles = await _userService.GetUserRolesAsync(userId, tenantId);
            if (!userRoles.IsSuccess)
            {
                return BadRequest(ResponseDto<List<Guid>>.Fail("Kullanıcı rolleri alınırken bir hata oluştu"));
            }
            
            return Ok(userRoles);
        }
    }
} 