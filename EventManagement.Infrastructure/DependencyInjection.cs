using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EventManagement.Application.Interfaces;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Repositories;

namespace EventManagement.Infrastructure
{
    /// <summary>
    /// Infrastructure katmanı için bağımlılık yönetimi yapılandırması
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Infrastructure servislerini kaydeder
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Veritabanı bağlantısı
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            // Repository sınıflarının kaydı
            services.AddScoped<IAttendeeRepository, AttendeeRepository>();
            
            return services;
        }
    }
} 