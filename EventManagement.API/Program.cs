using EventManagement.API;
using EventManagement.API.Extensions;
using EventManagement.API.Middleware;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Interfaces;
using EventManagement.Infrastructure.Authentication;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();

// Swagger yapılandırmasını kullan
builder.Services.AddSwaggerDocumentation();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSecretKeyForEventManagementAppToReplace"))
        };
    });

// Services DI Container'ına ekle
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();


// Redis Cache Service kullan
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost";
    options.InstanceName = "EventMgmt_";
});

// Redis ConnectionMultiplexer singleton olarak ekle
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
{
    var configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost";
    var configOptions = ConfigurationOptions.Parse(configuration);
    configOptions.AbortOnConnectFail = false; // Bağlantı başarısız olduğunda devam et
    configOptions.ConnectTimeout = 30000; // 30 saniye timeout
    configOptions.SyncTimeout = 30000; 
    configOptions.ConnectRetry = 5; // 5 kez bağlantıyı tekrar deneme
    return ConnectionMultiplexer.Connect(configOptions);
});

// Cache Service ekleyin
builder.Services.AddScoped<ICacheService, CacheService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.SetIsOriginAllowed(_ => true) // Tüm kaynaklardan gelen isteklere izin ver
               .AllowAnyMethod()              // Tüm HTTP metodlarına izin ver (GET, POST, PUT, DELETE vb.)
               .AllowAnyHeader()              // Tüm HTTP başlıklarına izin ver
               .AllowCredentials();           // Kimlik bilgilerinin paylaşılmasına izin ver
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Özel Swagger yapılandırmasını kullan
    app.UseSwaggerDocumentation();
}
else
{
    // HTTPS yönlendirmesini sadece üretim ortamında kullan
    app.UseHttpsRedirection();
}

// Statik dosyaları servis et (CSS, JS, vb.)
app.UseStaticFiles();

// CORS middleware'ini diğer middleware'lerden önce yerleştir
app.UseCors("AllowAll");

// TenantMiddleware kullanımı
app.UseTenantMiddleware();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Seed veritabanı
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
    await DbInitializer.Initialize(app.Services, logger);
}

app.Run();
