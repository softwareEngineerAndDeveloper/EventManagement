using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using Moq;
using System.Linq.Expressions;

namespace EventManagement.Test.Services
{
    public class EventServiceTests
    {
        private readonly Mock<IRepository<Event>> _eventRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly IEventService _eventService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IRepository<Event>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _unitOfWorkMock.Setup(uow => uow.Events).Returns(_eventRepositoryMock.Object);
            _cacheServiceMock = new Mock<ICacheService>();
            
            _eventService = new EventService(_unitOfWorkMock.Object, _cacheServiceMock.Object);
        }

        [Fact]
        public async Task GetEventById_WhenEventExists_ReturnsEvent()
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
                CreatorId = Guid.NewGuid(),
                TenantId = _tenantId
            };

            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(testEvent);

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(eventId, result.Data.Id);
            Assert.Equal("Test Event", result.Data.Title);
            Assert.Equal("Test Description", result.Data.Description);
        }

        [Fact]
        public async Task GetAllEvents_ReturnsAllEvents()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Event 1",
                    Description = "Description 1",
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(2),
                    Location = "Location 1",
                    MaxAttendees = 100,
                    IsPublic = true,
                    Status = EventStatus.Approved,
                    CreatorId = Guid.NewGuid(),
                    TenantId = _tenantId
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Event 2",
                    Description = "Description 2",
                    StartDate = DateTime.UtcNow.AddDays(3),
                    EndDate = DateTime.UtcNow.AddDays(4),
                    Location = "Location 2",
                    MaxAttendees = 50,
                    IsPublic = true,
                    Status = EventStatus.Approved,
                    CreatorId = Guid.NewGuid(),
                    TenantId = _tenantId
                }
            };

            _eventRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Event, bool>>>()))
                .ReturnsAsync(events);

            // Act
            var result = await _eventService.GetAllEventsAsync(_tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.Contains(result.Data, e => e.Title == "Event 1");
            Assert.Contains(result.Data, e => e.Title == "Event 2");
        }

        [Fact]
        public async Task CreateEvent_CreatesNewEvent()
        {
            // Arrange
            var createEventDto = new CreateEventDto
            {
                Title = "New Event",
                Description = "New Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "New Location",
                MaxAttendees = 100,
                IsPublic = true,
                CreatorId = Guid.NewGuid(),
                TenantId = _tenantId
            };

            var createdEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = createEventDto.Title,
                Description = createEventDto.Description,
                StartDate = createEventDto.StartDate,
                EndDate = createEventDto.EndDate,
                Location = createEventDto.Location,
                MaxAttendees = createEventDto.MaxAttendees,
                IsPublic = createEventDto.IsPublic,
                CreatorId = createEventDto.CreatorId,
                TenantId = createEventDto.TenantId,
                Status = EventStatus.Pending
            };

            Event addedEvent = null;
            _eventRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Event>()))
                .Callback<Event>(e => addedEvent = e);

            var createdEventDto = new EventDto
            {
                Id = createdEvent.Id,
                Title = createdEvent.Title,
                Description = createdEvent.Description,
                StartDate = createdEvent.StartDate,
                EndDate = createdEvent.EndDate,
                Location = createdEvent.Location,
                MaxAttendees = createdEvent.MaxAttendees,
                IsPublic = createdEvent.IsPublic,
                Status = createdEvent.Status,
                CreatorId = createdEvent.CreatorId,
                TenantId = createdEvent.TenantId
            };

            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(createdEvent);

            // Act
            var result = await _eventService.CreateEventAsync(createEventDto, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(createEventDto.Title, result.Data.Title);
            Assert.Equal(createEventDto.Description, result.Data.Description);
            Assert.Equal(createEventDto.MaxAttendees, result.Data.MaxAttendees);

            _eventRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Event>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }
    }
} 