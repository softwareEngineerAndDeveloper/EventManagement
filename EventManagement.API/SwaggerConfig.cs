using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace EventManagement.API
{
    public static class SwaggerConfig
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Etkinlik Yönetim API",
                    Version = "v1",
                    Description = "Etkinlik Yönetim Sistemi için çok kiracılı (multi-tenant) bir API",
                    Contact = new OpenApiContact
                    {
                        Name = "Destek Ekibi",
                        Email = "destek@etkinlikyonetim.com",
                        Url = new Uri("https://etkinlikyonetim.com/destek")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT Lisansı",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // JWT kimlik doğrulama için Swagger UI yapılandırması
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Yetkilendirme başlığı Bearer şeması kullanır. 
                    <br/>Önce /api/auth/login endpoint'i ile giriş yapıp token alın. 
                    <br/>Sonra aşağıdaki değeri şu şekilde girin: 'Bearer [alınan_token]'
                    <br/>Örnek: ""Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR...""",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Multi-tenant başlık parametrelerini ekle
                c.AddSecurityDefinition("X-Tenant", new OpenApiSecurityScheme
                {
                    Description = @"Kiracı alt alan adı (subdomain) başlığı. 
                    <br/>API'yi kullanmak için kiracı bilgisi sağlamanız gerekmektedir.
                    <br/>Bu alan ile tenantın subdomain değerini sağlayabilirsiniz (örneğin 'test')",
                    Name = "X-Tenant",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "X-Tenant"
                });

                c.AddSecurityDefinition("X-Tenant-ID", new OpenApiSecurityScheme
                {
                    Description = @"Kiracı ID başlığı (GUID formatında).
                    <br/>Bu alanı X-Tenant yerine alternatif olarak kullanabilirsiniz.
                    <br/>Örnek: '00000000-0000-0000-0000-000000000000'",
                    Name = "X-Tenant-ID",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "X-Tenant-ID"
                });

                // API controller'ları için XML belgelemeyi (dosyası varsa) ekle
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api-docs/{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    // Swagger UI'da API URL'yi güncelleme
                    swaggerDoc.Servers = new List<OpenApiServer> {
                        new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                    };
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api-docs/v1/swagger.json", "Etkinlik Yönetim API v1");
                c.RoutePrefix = "api-docs";
                c.DocumentTitle = "Etkinlik Yönetim API Dokümantasyonu";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.DefaultModelsExpandDepth(1);
                c.EnableDeepLinking();
                c.DisplayRequestDuration();
                c.EnableFilter();
                c.EnableValidator();
                
                // Execute özelliğini devre dışı bırak (kaldırıldı)
                // c.EnableTryItOutByDefault();
                c.SupportedSubmitMethods();
                // c.DisplayOperationId();
                
                // JSON syntax highlighting
                c.ConfigObject.AdditionalItems.Add("syntaxHighlight", true);
                
                // API anahtarı girişini kolaylaştır
                c.ConfigObject.AdditionalItems.Add("persistAuthorization", true);
            });

            return app;
        }
    }
} 