using EventManagement.API.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected Guid GetUserId()
        {
            return HttpContext.GetUserId();
        }
        
        protected Guid GetTenantId()
        {
            return HttpContext.GetTenantId();
        }
    }
} 