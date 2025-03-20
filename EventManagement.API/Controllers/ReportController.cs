using EventManagement.API.Extensions;
using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [Authorize]
    [Route("api/reports")]
    public class ReportController : BaseApiController
    {
        private readonly IEventService _eventService;
        
        public ReportController(IEventService eventService)
        {
            _eventService = eventService;
        }
        
        [HttpGet("upcoming-events")]
        public async Task<ActionResult<ResponseDto<List<EventDto>>>> GetUpcomingEventsReport()
        {
            var tenantId = GetTenantId();
            var result = await _eventService.GetUpcomingEventsAsync(tenantId);
            return Ok(result);
        }
    }
} 