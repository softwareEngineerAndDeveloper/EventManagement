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
        private readonly IUserService _userService;
        private readonly IRegistrationService _registrationService;
        
        public ReportController(
            IEventService eventService, 
            IUserService userService, 
            IRegistrationService registrationService)
        {
            _eventService = eventService;
            _userService = userService;
            _registrationService = registrationService;
        }
        
        [HttpGet("upcoming-events")]
        public async Task<ActionResult<ResponseDto<List<EventDto>>>> GetUpcomingEventsReport()
        {
            var tenantId = GetTenantId();
            var result = await _eventService.GetUpcomingEventsAsync(tenantId);
            return Ok(result);
        }
        
        [HttpGet("dashboard-stats")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<ActionResult<ResponseDto<DashboardStatsDto>>> GetDashboardStats()
        {
            var tenantId = GetTenantId();
            var isAdmin = User.IsInRole("Admin");
            
            // Admin için tüm istatistikler, EventManager için sadece kendi tenant'ına ait istatistikler
            var totalEvents = await _eventService.GetTotalEventsCountAsync(isAdmin ? null : tenantId);
            var activeEvents = await _eventService.GetActiveEventsCountAsync(isAdmin ? null : tenantId);
            var upcomingEvents = await _eventService.GetUpcomingEventsCountAsync(isAdmin ? null : tenantId);
            var totalRegistrations = await _registrationService.GetTotalRegistrationsCountAsync(isAdmin ? null : tenantId);
            var totalUsers = isAdmin 
                ? await _userService.GetTotalUsersCountAsync() 
                : await _userService.GetTotalUsersByTenantAsync(tenantId);
            
            var dashboardStats = new DashboardStatsDto
            {
                TotalEvents = totalEvents,
                ActiveEvents = activeEvents,
                UpcomingEvents = upcomingEvents,
                TotalRegistrations = totalRegistrations,
                TotalUsers = totalUsers
            };
            
            return Ok(ResponseDto<DashboardStatsDto>.Success(dashboardStats, "Dashboard istatistikleri başarıyla alındı."));
        }
    }
} 