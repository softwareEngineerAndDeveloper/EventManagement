using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using Moq;
using System.Linq.Expressions;
using System.Text.Json;

namespace EventManagement.Test.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly IUserService _userService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IRepository<User>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            _cacheServiceMock = new Mock<ICacheService>();
            
            _unitOfWorkMock.Setup(uow => uow.Users).Returns(_userRepositoryMock.Object);
            
            _userService = new UserService(_unitOfWorkMock.Object, _jwtTokenGeneratorMock.Object, _cacheServiceMock.Object);
        }

        [Fact]
        public async Task GetUserById_WhenUserExists_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = new User
            {
                Id = userId,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PhoneNumber = "5551234567",
                PasswordHash = "hashedPassword",
                TenantId = _tenantId
            };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(testUser);

            // Act
            var result = await _userService.GetUserByIdAsync(userId, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(userId, result.Data.Id);
            Assert.Equal("Test", result.Data.FirstName);
            Assert.Equal("User", result.Data.LastName);
            Assert.Equal("test@example.com", result.Data.Email);
        }

        [Fact]
        public async Task GetUserById_WhenUserDoesNotExist_ReturnsFailResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId, _tenantId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Contains($"Kullanıcı bulunamadı (ID: {userId})", result.Message);
        }

        [Fact]
        public async Task GetUserByEmail_WhenUserExists_ReturnsUser()
        {
            // Arrange
            var email = "test@example.com";
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = email,
                PhoneNumber = "5551234567",
                PasswordHash = "hashedPassword",
                TenantId = _tenantId
            };

            _userRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new List<User> { testUser });

            // Act
            var result = await _userService.GetUserByEmailAsync(email, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(email, result.Data.Email);
            Assert.Equal("Test", result.Data.FirstName);
            Assert.Equal("User", result.Data.LastName);
        }

        [Fact]
        public async Task CreateUser_CreatesNewUser()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                FirstName = "New",
                LastName = "User",
                Email = "new@example.com",
                Password = "P@ssword123",
                ConfirmPassword = "P@ssword123",
                PhoneNumber = "5559876543"
            };

            _userRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new List<User>());

            User addedUser = null;
            _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => addedUser = u)
                .ReturnsAsync(new User 
                {
                    Id = Guid.NewGuid(),
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    Email = createUserDto.Email,
                    PhoneNumber = createUserDto.PhoneNumber,
                    PasswordHash = "hashedPassword",
                    TenantId = _tenantId
                });

            // Act
            var result = await _userService.CreateUserAsync(createUserDto, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(createUserDto.FirstName, result.Data.FirstName);
            Assert.Equal(createUserDto.LastName, result.Data.LastName);
            Assert.Equal(createUserDto.Email, result.Data.Email);
            Assert.Equal(createUserDto.PhoneNumber, result.Data.PhoneNumber);
            
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_WhenUserExists_UpdatesUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = userId,
                FirstName = "Existing",
                LastName = "User",
                Email = "existing@example.com",
                PhoneNumber = "5551234567",
                PasswordHash = "hashedPassword",
                TenantId = _tenantId
            };

            var updateUserDto = new UpdateUserDto
            {
                FirstName = "Updated",
                LastName = "User",
                PhoneNumber = "5559876543",
                IsActive = true
            };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);
                
            _userRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new List<User> { existingUser });

            // Act
            var result = await _userService.UpdateUserAsync(userId, updateUserDto, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(updateUserDto.FirstName, result.Data.FirstName);
            Assert.Equal(updateUserDto.LastName, result.Data.LastName);
            Assert.Equal(updateUserDto.PhoneNumber, result.Data.PhoneNumber);
            Assert.Equal("existing@example.com", result.Data.Email); // Email değişmemeli
            
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenValidCredentials_ReturnsToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "P@ssword123",
                Subdomain = "test"
            };

            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = loginDto.Email,
                PhoneNumber = "5551234567",
                PasswordHash = "hashedPassword",
                TenantId = _tenantId
            };

            _userRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new List<User> { existingUser });

            var token = "test_jwt_token";
            _jwtTokenGeneratorMock.Setup(j => j.GenerateTokenAsync(It.IsAny<User>()))
                .ReturnsAsync(token);

            // VerifyPassword metodu UserService içinde olduğu için bu testi yapabilmek için
            // metod izole edilmeli veya yeniden yazılmalı (gerçek implementasyonu)
            
            // Act
            var result = await _userService.AuthenticateAsync(loginDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(token, result.Data);
        }

        [Fact]
        public async Task GetTotalUsersCountAsync_ReturnsCachedValue()
        {
            // Arrange
            var expectedCount = 10;
            var cacheKey = "users:count:total";

            _cacheServiceMock.Setup(c => c.GetOrSetAsync(
                    It.Is<string>(s => s == cacheKey),
                    It.IsAny<Func<Task<int>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _userService.GetTotalUsersCountAsync();

            // Assert
            Assert.Equal(expectedCount, result);
            _cacheServiceMock.Verify(c => c.GetOrSetAsync(
                It.Is<string>(s => s == cacheKey),
                It.IsAny<Func<Task<int>>>(),
                It.IsAny<TimeSpan>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetTotalUsersByTenantAsync_ReturnsCachedValue()
        {
            // Arrange
            var expectedCount = 5;
            var cacheKey = $"tenant:{_tenantId}:users:count";

            _cacheServiceMock.Setup(c => c.GetOrSetAsync(
                    It.Is<string>(s => s == cacheKey),
                    It.IsAny<Func<Task<int>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _userService.GetTotalUsersByTenantAsync(_tenantId);

            // Assert
            Assert.Equal(expectedCount, result);
            _cacheServiceMock.Verify(c => c.GetOrSetAsync(
                It.Is<string>(s => s == cacheKey),
                It.IsAny<Func<Task<int>>>(),
                It.IsAny<TimeSpan>()), 
                Times.Once);
        }
    }
} 