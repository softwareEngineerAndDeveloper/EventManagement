using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using Moq;
using System.Linq.Expressions;

namespace EventManagement.Test.Services
{
    public class RegistrationServiceTests
    {
        private readonly Mock<IRepository<Registration>> _registrationRepositoryMock;
        private readonly Mock<IRepository<Event>> _eventRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly IRegistrationService _registrationService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public RegistrationServiceTests()
        {
            _registrationRepositoryMock = new Mock<IRepository<Registration>>();
            _eventRepositoryMock = new Mock<IRepository<Event>>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            
            _unitOfWorkMock.Setup(uow => uow.Registrations).Returns(_registrationRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.Events).Returns(_eventRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.Users).Returns(_userRepositoryMock.Object);
            
            _registrationService = new RegistrationService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetRegistrationById_WhenRegistrationExists_ReturnsRegistration()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 100,
                IsPublic = true,
                Status = EventStatus.Approved,
                TenantId = _tenantId
            };
            
            var testRegistration = new Registration
            {
                Id = registrationId,
                EventId = eventId,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow,
                HasAttended = false,
                IsCancelled = false,
                Event = testEvent
            };

            _registrationRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Registration, bool>>>()))
                .ReturnsAsync(new List<Registration> { testRegistration });

            // Act
            var result = await _registrationService.GetRegistrationByIdAsync(registrationId, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(registrationId, result.Data.Id);
            Assert.Equal(eventId, result.Data.EventId);
            Assert.Equal(userId, result.Data.UserId);
            Assert.False(result.Data.HasAttended);
            Assert.False(result.Data.IsCancelled);
        }

        [Fact]
        public async Task GetRegistrationsByEventId_ReturnsRegistrationsForEvent()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 100,
                IsPublic = true,
                Status = EventStatus.Approved,
                TenantId = _tenantId
            };
            
            var registrations = new List<Registration>
            {
                new Registration
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = Guid.NewGuid(),
                    RegistrationDate = DateTime.UtcNow,
                    HasAttended = false,
                    IsCancelled = false,
                    Event = testEvent
                },
                new Registration
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = Guid.NewGuid(),
                    RegistrationDate = DateTime.UtcNow,
                    HasAttended = false,
                    IsCancelled = false,
                    Event = testEvent
                }
            };

            _registrationRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Registration, bool>>>()))
                .ReturnsAsync(registrations);

            // Act
            var result = await _registrationService.GetRegistrationsByEventIdAsync(eventId, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.All(result.Data, r => Assert.Equal(eventId, r.EventId));
        }

        [Fact]
        public async Task CreateRegistration_CreatesNewRegistration()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createRegistrationDto = new CreateRegistrationDto
            {
                EventId = eventId,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PhoneNumber = "5551234567",
                AdditionalNotes = "Test Notes"
            };

            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 100,
                IsPublic = true,
                Status = EventStatus.Approved,
                TenantId = _tenantId
            };
            
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

            _eventRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Event, bool>>>()))
                .ReturnsAsync(new List<Event> { testEvent });
                
            _userRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new List<User> { testUser });
                
            _registrationRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Registration, bool>>>()))
                .ReturnsAsync(new List<Registration>());

            Registration createdRegistration = null;
            _registrationRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Registration>()))
                .Callback<Registration>(r => createdRegistration = r)
                .ReturnsAsync(new Registration
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = userId,
                    AdditionalNotes = createRegistrationDto.AdditionalNotes,
                    RegistrationDate = DateTime.UtcNow,
                    HasAttended = false,
                    IsCancelled = false,
                    Event = testEvent,
                    User = testUser
                });

            // Act
            var result = await _registrationService.CreateRegistrationAsync(createRegistrationDto, _tenantId, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(eventId, result.Data.EventId);
            Assert.Equal(userId, result.Data.UserId);
            Assert.Equal(createRegistrationDto.AdditionalNotes, result.Data.AdditionalNotes);
            Assert.False(result.Data.HasAttended);
            Assert.False(result.Data.IsCancelled);
            
            _registrationRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Registration>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelRegistration_WhenRegistrationExists_MarksRegistrationAsCancelled()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 100,
                IsPublic = true,
                Status = EventStatus.Approved,
                TenantId = _tenantId
            };
            
            var registration = new Registration
            {
                Id = registrationId,
                EventId = eventId,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow,
                HasAttended = false,
                IsCancelled = false,
                Event = testEvent
            };

            _registrationRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Registration, bool>>>()))
                .ReturnsAsync(new List<Registration> { registration });

            // Act
            var result = await _registrationService.CancelRegistrationAsync(registrationId, _tenantId, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.True(registration.IsCancelled);
            
            _registrationRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Registration>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkAttendance_WhenRegistrationExists_UpdatesAttendanceStatus()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 100,
                IsPublic = true,
                Status = EventStatus.Approved,
                TenantId = _tenantId
            };
            
            var registration = new Registration
            {
                Id = registrationId,
                EventId = eventId,
                UserId = Guid.NewGuid(),
                RegistrationDate = DateTime.UtcNow,
                HasAttended = false,
                IsCancelled = false,
                Event = testEvent
            };

            _registrationRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Registration, bool>>>()))
                .ReturnsAsync(new List<Registration> { registration });

            // Act
            var result = await _registrationService.MarkAttendanceAsync(registrationId, true, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.True(registration.HasAttended);
            
            _registrationRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Registration>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }
    }
} 