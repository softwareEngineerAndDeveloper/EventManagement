using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using EventManagement.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace EventManagement.Test.Middleware
{
    public class TenantMiddlewareTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<Tenant>> _tenantRepositoryMock;
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly TenantMiddleware _middleware;
        private readonly HttpContext _httpContext;

        public TenantMiddlewareTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tenantRepositoryMock = new Mock<IRepository<Tenant>>();
            _nextMock = new Mock<RequestDelegate>();
            _middleware = new TenantMiddleware(_nextMock.Object);
            
            _httpContext = new DefaultHttpContext();
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IUnitOfWork)))
                .Returns(_unitOfWorkMock.Object);
                
            _httpContext.RequestServices = serviceProviderMock.Object;
            
            _unitOfWorkMock.Setup(uow => uow.Tenants).Returns(_tenantRepositoryMock.Object);
        }

        [Fact]
        public async Task InvokeAsync_TenantIdHeader_FindsTenantAndSetsInContext()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = "Test Tenant",
                Description = "Test Description",
                Subdomain = "test",
                ContactEmail = "test@example.com",
                ContactPhone = "5551234567",
                IsActive = true
            };
            
            _httpContext.Request.Headers["X-Tenant-ID"] = tenantId.ToString();
            
            _tenantRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>()))
                .ReturnsAsync(new List<Tenant> { tenant });
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            Assert.NotNull(_httpContext.Items["Tenant"]);
            Assert.Equal(tenant, _httpContext.Items["Tenant"]);
            _nextMock.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_TenantSubdomainHeader_FindsTenantAndSetsInContext()
        {
            // Arrange
            var subdomain = "test";
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Test Tenant",
                Description = "Test Description",
                Subdomain = subdomain,
                ContactEmail = "test@example.com",
                ContactPhone = "5551234567",
                IsActive = true
            };
            
            _httpContext.Request.Headers["X-Tenant"] = subdomain;
            
            _tenantRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>()))
                .ReturnsAsync(new List<Tenant> { tenant });
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            Assert.NotNull(_httpContext.Items["Tenant"]);
            Assert.Equal(tenant, _httpContext.Items["Tenant"]);
            _nextMock.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_TenantNotFound_Returns404ForNonAuthEndpoints()
        {
            // Arrange
            _httpContext.Request.Path = "/api/events";
            _tenantRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>()))
                .ReturnsAsync(new List<Tenant>());
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, _httpContext.Response.StatusCode);
            _nextMock.Verify(next => next(_httpContext), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_TenantNotFound_AllowsAuthEndpoints()
        {
            // Arrange
            _httpContext.Request.Path = "/api/Auth/login";
            _tenantRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>()))
                .ReturnsAsync(new List<Tenant>());
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            Assert.NotEqual(StatusCodes.Status404NotFound, _httpContext.Response.StatusCode);
            _nextMock.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_TenantMismatch_Returns403()
        {
            // Arrange
            var userTenantId = Guid.NewGuid();
            var requestTenantId = Guid.NewGuid();
            
            var tenant = new Tenant
            {
                Id = requestTenantId,
                Name = "Request Tenant",
                Description = "Request Description",
                Subdomain = "request",
                ContactEmail = "request@example.com",
                ContactPhone = "5551234567",
                IsActive = true
            };
            
            // Kullanıcı tenant ID'si
            var userClaims = new List<Claim>
            {
                new Claim("tenant_id", userTenantId.ToString())
            };
            var identity = new ClaimsIdentity(userClaims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _httpContext.User = claimsPrincipal;
            
            // Request header tenant ID'si
            _httpContext.Request.Headers["X-Tenant-ID"] = requestTenantId.ToString();
            
            _tenantRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>()))
                .ReturnsAsync(new List<Tenant> { tenant });
            
            // Act
            await _middleware.InvokeAsync(_httpContext);
            
            // Assert
            Assert.Equal(StatusCodes.Status403Forbidden, _httpContext.Response.StatusCode);
            _nextMock.Verify(next => next(_httpContext), Times.Never);
        }
    }
} 