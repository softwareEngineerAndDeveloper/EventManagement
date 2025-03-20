using EventManagement.Application.DTOs;
using EventManagement.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace EventManagement.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;
        
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                
                var statusCode = HttpStatusCode.InternalServerError;
                var message = "Bir hata oluştu.";
                var errors = new List<string>();
                
                if (ex is NotFoundException)
                {
                    statusCode = HttpStatusCode.NotFound;
                    message = ex.Message;
                }
                else if (ex is ValidationException validationEx)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    message = ex.Message;
                    errors = validationEx.Errors;
                }
                else if (ex is UnauthorizedException)
                {
                    statusCode = HttpStatusCode.Unauthorized;
                    message = ex.Message;
                }
                else if (ex is TenantMismatchException)
                {
                    statusCode = HttpStatusCode.Forbidden;
                    message = ex.Message;
                }
                
                context.Response.StatusCode = (int)statusCode;
                
                var response = ResponseDto<object>.Fail(message, errors);
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);
                
                await context.Response.WriteAsync(json);
            }
        }
    }
} 