using EventManagement.Domain.Entities;
using System;
using Xunit;

namespace EventManagement.Test.Domain
{
    public class EventTests
    {
        [Fact]
        public void Event_Creation_PropertiesSetCorrectly()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var title = "Test Etkinliği";
            var description = "Test açıklaması";
            var location = "Test lokasyonu";
            var startDate = DateTime.UtcNow.AddDays(1);
            var endDate = DateTime.UtcNow.AddDays(2);
            var maxAttendees = 100;

            // Act
            var eventEntity = new Event
            {
                Id = eventId,
                Title = title,
                Description = description,
                Location = location,
                StartDate = startDate,
                EndDate = endDate,
                MaxAttendees = maxAttendees,
                IsPublic = true,
                Status = EventStatus.Approved,
                CreatorId = creatorId,
                TenantId = tenantId
            };

            // Assert
            Assert.Equal(eventId, eventEntity.Id);
            Assert.Equal(title, eventEntity.Title);
            Assert.Equal(description, eventEntity.Description);
            Assert.Equal(location, eventEntity.Location);
            Assert.Equal(startDate, eventEntity.StartDate);
            Assert.Equal(endDate, eventEntity.EndDate);
            Assert.Equal(maxAttendees, eventEntity.MaxAttendees);
            Assert.True(eventEntity.IsPublic);
            Assert.Equal(EventStatus.Approved, eventEntity.Status);
            Assert.Equal(creatorId, eventEntity.CreatorId);
            Assert.Equal(tenantId, eventEntity.TenantId);
        }

        [Fact]
        public void Event_WithNegativeMaxAttendees_ThrowsException()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Event
            {
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                MaxAttendees = -10,
                CreatorId = Guid.NewGuid(),
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("must be greater than or equal to zero", exception.Message);
        }

        [Fact]
        public void Event_WithEndDateBeforeStartDate_ThrowsException()
        {
            // Arrange & Act & Assert
            var startDate = DateTime.UtcNow.AddDays(2);
            var endDate = DateTime.UtcNow.AddDays(1);

            var exception = Assert.Throws<ArgumentException>(() => new Event
            {
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                StartDate = startDate,
                EndDate = endDate,
                MaxAttendees = 100,
                CreatorId = Guid.NewGuid(),
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("must be after the start date", exception.Message);
        }

        [Fact]
        public void Event_WithPastStartDate_ThrowsException()
        {
            // Arrange & Act & Assert
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow.AddDays(1);

            var exception = Assert.Throws<ArgumentException>(() => new Event
            {
                Title = "Test Etkinliği",
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                StartDate = startDate,
                EndDate = endDate,
                MaxAttendees = 100,
                CreatorId = Guid.NewGuid(),
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("must be in the future", exception.Message);
        }

        [Fact]
        public void Event_WithTitleTooLong_ThrowsException()
        {
            // Arrange & Act & Assert
            var veryLongTitle = new string('A', 201); // 201 karakter

            var exception = Assert.Throws<ArgumentException>(() => new Event
            {
                Title = veryLongTitle,
                Description = "Test açıklaması",
                Location = "Test lokasyonu",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                MaxAttendees = 100,
                CreatorId = Guid.NewGuid(),
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("length cannot exceed 200 characters", exception.Message);
        }
    }
} 