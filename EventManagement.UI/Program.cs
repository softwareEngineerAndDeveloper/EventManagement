using EventManagement.UI.Services;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpClient için servis
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// ApiClient yapılandırması
builder.Services.AddHttpClient("ApiClient", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5294/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    
    // X-Tenant header'ı ekle
    var defaultTenant = builder.Configuration["ApiSettings:DefaultTenant"];
    if (!string.IsNullOrEmpty(defaultTenant))
    {
        client.DefaultRequestHeaders.Add("X-Tenant", defaultTenant);
    }
});

// Servisler
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// UI Servisleri - Sadece tanımlanmış olanlar
builder.Services.AddHttpClient<IUserService, UserService>();
builder.Services.AddHttpClient<IRoleService, RoleService>();


// Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// Cookie tabanlı kimlik doğrulama ekle
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        
        // Cookie'nin silinmesini kolaylaştırır
        options.Cookie.Name = "EventManagement.Auth";
        
        // Çıkış yaparken cookie'yi otomatik sil
        options.SlidingExpiration = true;
        
        // Yetkilendirme başarısız olduğunda yeniden girişe yönlendir
        options.Events.OnRedirectToLogin = context =>
        {
            if (!context.Response.Headers.ContainsKey("Cache-Control"))
                context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            
            if (!context.Response.Headers.ContainsKey("Pragma"))
                context.Response.Headers.Append("Pragma", "no-cache");
            
            if (!context.Response.Headers.ContainsKey("Expires"))
                context.Response.Headers.Append("Expires", "0");
                
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        
        // Cookie geçersiz olduğunda her zaman giriş sayfasına yönlendir
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (!context.Response.Headers.ContainsKey("Cache-Control"))
                context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            
            if (!context.Response.Headers.ContainsKey("Pragma"))
                context.Response.Headers.Append("Pragma", "no-cache");
            
            if (!context.Response.Headers.ContainsKey("Expires"))
                context.Response.Headers.Append("Expires", "0");
                
            // Giriş sayfasına yönlendir
            context.Response.Redirect("/Account/Login");
            return Task.CompletedTask;
        };
    });

// Oturum yapılandırması
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "EventManagement.Session";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Üretim ortamında genel hata sayfasına yönlendir
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Geliştirme ortamında bile kullanıcı dostu hata sayfası göster
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Kimlik doğrulama ve yetkilendirme middleware'leri
app.UseAuthentication();
app.UseAuthorization();

// Oturum middleware'i
app.UseSession();

// Yerel alan (Area) yönlendirmeleri
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Varsayılan Yönlendirme
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
