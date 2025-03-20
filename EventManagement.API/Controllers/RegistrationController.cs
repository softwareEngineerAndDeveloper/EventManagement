using EventManagement.API.Extensions;
using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [Route("api/registrations")]
    public class RegistrationController : BaseApiController
    {
        private readonly IRegistrationService _registrationService;
        
        public RegistrationController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseDto<RegistrationDto>>> GetRegistrationById(Guid id)
        {
            var tenantId = GetTenantId();
            var result = await _registrationService.GetRegistrationByIdAsync(id, tenantId);
            return Ok(result);
        }
        
        [HttpGet("user/me")]
        [Authorize]
        public async Task<ActionResult<ResponseDto<List<RegistrationDto>>>> GetMyRegistrations()
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();
            var result = await _registrationService.GetRegistrationsByUserIdAsync(userId, tenantId);
            return Ok(result);
        }
    }
} 