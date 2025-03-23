using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using Moq;
using System.Linq.Expressions;

namespace EventManagement.Test.Services
{
    public class TenantServiceTests
    {
        private readonly Mock<IRepository<Tenant>> _tenantRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly ITenantService _tenantService;

        public TenantServiceTests()
        {
            _tenantRepositoryMock = new Mock<IRepository<Tenant>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _unitOfWorkMock.Setup(uow => uow.Tenants).Returns(_tenantRepositoryMock.Object);
            
            _tenantService = new TenantService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetTenantById_WhenTenantExists_ReturnsTenant()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var testTenant = new Tenant
            {
                Id = tenantId,
                Name = "Test Tenant",
                Description = "Test Description",
                Subdomain = "test",
                ContactEmail = "test@example.com",
                ContactPhone = "5551234567",
                IsActive = true
            };

            _tenantRepositoryMock.Setup(repo => repo.GetByIdAsync(tenantId))
                .ReturnsAsync(testTenant);

            // Act
            var result = await _tenantService.GetTenantByIdAsync(tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(tenantId, result.Data.Id);
            Assert.Equal("Test Tenant", result.Data.Name);
            Assert.Equal("test", result.Data.Subdomain);
        }

        [Fact]
        public async Task GetTenantBySubdomain_WhenTenantExists_ReturnsTenant()
        {
            // Arrange
            var subdomain = "test";
            var testTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Test Tenant",
                Description = "Test Description",
                Subdomain = subdomain,
                ContactEmail = "test@example.com",
                ContactPhone = "5551234567",
                IsActive = true
            };

            _tenantRepositoryMock.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
                .ReturnsAsync(new List<Tenant> { testTenant });

            // Act
            var result = await _tenantService.GetTenantBySubdomainAsync(subdomain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(subdomain, result.Data.Subdomain);
            Assert.Equal("Test Tenant", result.Data.Name);
        }

        [Fact]
        public async Task CreateTenant_CreatesTenant()
        {
            // Arrange
            var createTenantDto = new CreateTenantDto
            {
                Name = "New Tenant",
                Description = "New Description",
                Subdomain = "new",
                ContactEmail = "new@example.com",
                ContactPhone = "5551234567"
            };

            Tenant createdTenant = null;
            _tenantRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Tenant>()))
                .Callback<Tenant>(t => createdTenant = t);

            // Act
            var result = await _tenantService.CreateTenantAsync(createTenantDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(createTenantDto.Name, result.Data.Name);
            Assert.Equal(createTenantDto.Subdomain, result.Data.Subdomain);
            
            _tenantRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Tenant>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateTenant_WhenTenantExists_UpdatesTenant()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var existingTenant = new Tenant
            {
                Id = tenantId,
                Name = "Existing Tenant",
                Description = "Existing Description",
                Subdomain = "existing",
                ContactEmail = "existing@example.com",
                ContactPhone = "5551234567",
                IsActive = true
            };

            var updateTenantDto = new UpdateTenantDto
            {
                Name = "Updated Tenant",
                Description = "Updated Description",
                ContactEmail = "updated@example.com",
                ContactPhone = "5559876543",
                IsActive = true
            };

            _tenantRepositoryMock.Setup(repo => repo.GetByIdAsync(tenantId))
                .ReturnsAsync(existingTenant);

            // Act
            var result = await _tenantService.UpdateTenantAsync(tenantId, updateTenantDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(updateTenantDto.Name, result.Data.Name);
            Assert.Equal(updateTenantDto.Description, result.Data.Description);
            Assert.Equal(updateTenantDto.ContactEmail, result.Data.ContactEmail);
            Assert.Equal("existing", result.Data.Subdomain); // Subdomain değişmemeli
            
            _tenantRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Tenant>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteTenant_WhenTenantExists_SetsTenantInactive()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var existingTenant = new Tenant
            {
                Id = tenantId,
                Name = "Existing Tenant",
                Description = "Existing Description",
                Subdomain = "existing",
                ContactEmail = "existing@example.com",
                ContactPhone = "5551234567",
                IsActive = true
            };

            _tenantRepositoryMock.Setup(repo => repo.GetByIdAsync(tenantId))
                .ReturnsAsync(existingTenant);

            // Act
            var result = await _tenantService.DeleteTenantAsync(tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.False(existingTenant.IsActive);
            
            _tenantRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Tenant>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }
    }
} 