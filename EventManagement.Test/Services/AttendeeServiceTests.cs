using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventManagement.Test.Services
{
    public class AttendeeServiceTests
    {
        private readonly Mock<IAttendeeRepository> _attendeeRepositoryMock;
        private readonly Mock<ILogger<AttendeeService>> _loggerMock;
        private readonly IAttendeeService _attendeeService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public AttendeeServiceTests()
        {
            _attendeeRepositoryMock = new Mock<IAttendeeRepository>();
            _loggerMock = new Mock<ILogger<AttendeeService>>();
            
            _attendeeService = new AttendeeService(_attendeeRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAttendeesByEventId_WhenEventExists_ReturnsAttendees()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                TenantId = _tenantId
            };

            var attendees = new List<Attendee>
            {
                new Attendee
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Name = "Test Katılımcı 1",
                    Email = "test1@example.com",
                    Phone = "5551112233",
                    TenantId = _tenantId
                },
                new Attendee
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Name = "Test Katılımcı 2",
                    Email = "test2@example.com",
                    Phone = "5551112244",
                    TenantId = _tenantId
                }
            };

            _attendeeRepositoryMock.Setup(repo => repo.GetEventByIdAsync(eventId))
                .ReturnsAsync(testEvent);

            _attendeeRepositoryMock.Setup(repo => repo.GetAttendeesByEventIdAsync(eventId))
                .ReturnsAsync(attendees);

            // Act
            var result = await _attendeeService.GetAttendeesByEventIdAsync(eventId, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.Contains(result.Data, a => a.Name == "Test Katılımcı 1");
            Assert.Contains(result.Data, a => a.Name == "Test Katılımcı 2");
        }

        [Fact]
        public async Task GetAttendeesByEventId_WhenEventDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _attendeeRepositoryMock.Setup(repo => repo.GetEventByIdAsync(eventId))
                .ReturnsAsync((Event)null);

            // Act
            var result = await _attendeeService.GetAttendeesByEventIdAsync(eventId, _tenantId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Etkinlik bulunamadı", result.Message);
        }

        [Fact]
        public async Task GetAttendeesByEventId_WhenWrongTenant_ReturnsFailure()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var wrongTenantId = Guid.NewGuid();
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                TenantId = _tenantId
            };

            _attendeeRepositoryMock.Setup(repo => repo.GetEventByIdAsync(eventId))
                .ReturnsAsync(testEvent);

            // Act
            var result = await _attendeeService.GetAttendeesByEventIdAsync(eventId, wrongTenantId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Bu etkinliğe erişim izniniz yok", result.Message);
        }

        [Fact]
        public async Task CreateAttendee_WhenValidData_CreatesAndReturnsAttendee()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                TenantId = _tenantId
            };

            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "Yeni Katılımcı",
                Email = "yeni@example.com",
                Phone = "5551112255",
                Notes = "Test notları"
            };

            var createdAttendee = new Attendee
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Name = createAttendeeDto.Name,
                Email = createAttendeeDto.Email,
                Phone = createAttendeeDto.Phone,
                Status = 0, // Onaylandı
                RegistrationDate = DateTime.UtcNow,
                Notes = createAttendeeDto.Notes,
                TenantId = _tenantId
            };

            _attendeeRepositoryMock.Setup(repo => repo.GetEventByIdAsync(eventId))
                .ReturnsAsync(testEvent);

            _attendeeRepositoryMock.Setup(repo => repo.SearchAttendeeByEmailAsync(createAttendeeDto.Email))
                .ReturnsAsync((Attendee)null);

            _attendeeRepositoryMock.Setup(repo => repo.CreateAttendeeAsync(It.IsAny<Attendee>()))
                .ReturnsAsync(createdAttendee);

            // Act
            var result = await _attendeeService.CreateAttendeeAsync(createAttendeeDto, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(createAttendeeDto.Name, result.Data.Name);
            Assert.Equal(createAttendeeDto.Email, result.Data.Email);
            Assert.Equal(createAttendeeDto.Phone, result.Data.Phone);
            Assert.Equal("Katılımcı başarıyla oluşturuldu", result.Message);
        }

        [Fact]
        public async Task CreateAttendee_WhenEmailExists_ReturnsFailure()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                TenantId = _tenantId
            };

            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "Yeni Katılımcı",
                Email = "mevcut@example.com",
                Phone = "5551112255"
            };

            var existingAttendee = new Attendee
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Name = "Mevcut Katılımcı",
                Email = createAttendeeDto.Email,
                IsDeleted = false
            };

            _attendeeRepositoryMock.Setup(repo => repo.GetEventByIdAsync(eventId))
                .ReturnsAsync(testEvent);

            _attendeeRepositoryMock.Setup(repo => repo.SearchAttendeeByEmailAsync(createAttendeeDto.Email))
                .ReturnsAsync(existingAttendee);

            // Act
            var result = await _attendeeService.CreateAttendeeAsync(createAttendeeDto, _tenantId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Bu e-posta adresi ile etkinliğe kayıtlı bir katılımcı zaten mevcut", result.Message);
        }

        [Fact]
        public async Task GetAttendeeById_WhenAttendeeExists_ReturnsAttendee()
        {
            // Arrange
            var attendeeId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            
            var testEvent = new Event
            {
                Id = eventId,
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                TenantId = _tenantId
            };
            
            var testAttendee = new Attendee
            {
                Id = attendeeId,
                EventId = eventId,
                Name = "Test Katılımcı",
                Email = "test@example.com",
                Phone = "5551112233",
                TenantId = _tenantId
            };

            _attendeeRepositoryMock.Setup(repo => repo.GetAttendeeByIdAsync(attendeeId))
                .ReturnsAsync(testAttendee);
                
            _attendeeRepositoryMock.Setup(repo => repo.GetEventByIdAsync(eventId))
                .ReturnsAsync(testEvent);

            // Act
            var result = await _attendeeService.GetAttendeeByIdAsync(attendeeId, _tenantId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(attendeeId, result.Data.Id);
            Assert.Equal("Test Katılımcı", result.Data.Name);
            Assert.Equal("test@example.com", result.Data.Email);
        }
    }
} 