using Microsoft.Extensions.DependencyInjection;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;

namespace EventManagement.Application
{
    /// <summary>
    /// Application katmanı için bağımlılık yönetimi yapılandırması
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Application servislerini kaydeder
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Servis sınıflarının kaydı
            services.AddScoped<IAttendeeService, AttendeeService>();
            
            return services;
        }
    }
} 