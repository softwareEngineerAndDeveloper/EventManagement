using EventManagement.UI.Services;
using EventManagement.UI.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using EventManagement.UI.Models;
using EventManagement.UI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// API yapılandırması
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<TenantSettings>(builder.Configuration.GetSection("Tenant"));

// HttpClient yapılandırması
builder.Services.AddHttpClient<IApiServiceUI, ApiServiceUI>();
builder.Services.AddHttpContextAccessor();

// Caching
builder.Services.AddMemoryCache();

// Session yapılandırması
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Kimlik doğrulama
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "EventManagement.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

// Otorizasyon politikaları
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireEventManager", policy => policy.RequireRole("Admin", "EventManager"));
});

// API servisini DI container'a ekle
builder.Services.AddScoped<IApiServiceUI, ApiServiceUI>();
builder.Services.AddScoped<ITenantResolverService, TenantResolverService>();

// Admin Controller için gerekli servisleri ekle
builder.Services.AddScoped<IEventServiceUI, EventServiceUI>();
builder.Services.AddScoped<IUserServiceUI, UserServiceUI>();
builder.Services.AddScoped<IRoleServiceUI, RoleServiceUI>();
builder.Services.AddScoped<IAttendeeServiceUI, AttendeeServiceUI>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session middleware'ini ekle
app.UseSession();

// Tenant middleware'ini ekle
app.UseTenantMiddleware();

app.UseAuthentication();
app.UseAuthorization();

// AuthenticationMiddleware'den sonra HomeController için default route ekleyelim
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value == "/" || context.Request.Path.Value == "/Home/Index")
    {
        // Kullanıcı giriş yapmışsa ve Admin veya EventManager ise Admin dashboard'a yönlendir
        if (context.User.Identity?.IsAuthenticated == true && 
            (context.User.IsInRole("Admin") || context.User.IsInRole("EventManager")))
        {
            context.Response.Redirect("/Admin/Index");
            return;
        }
    }
    
    await next();
});

// Hata middleware'i
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Hata oluştuğunda kullanıcıya daha iyi bir mesaj göster
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "İstek işlenirken bir hata oluştu: {Path}", context.Request.Path);
        
        context.Response.Redirect("/Home/Error");
    }
});

// URL path tabanlı tenant route yapılandırması
app.MapControllerRoute(
    name: "tenant",
    pattern: "{tenant}/{controller=Home}/{action=Index}/{id?}");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Tenant ayarları için yardımcı sınıf
public class TenantSettings
{
    public string DefaultSubdomain { get; set; } = string.Empty;
}
