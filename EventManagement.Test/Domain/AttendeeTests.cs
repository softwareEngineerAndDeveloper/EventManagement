using EventManagement.Domain.Entities;
using System;
using Xunit;

namespace EventManagement.Test.Domain
{
    public class AttendeeTests
    {
        [Fact]
        public void Attendee_Creation_PropertiesSetCorrectly()
        {
            // Arrange
            var attendeeId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var name = "Test Katılımcı";
            var email = "test@example.com";
            var phone = "5551112233";
            var notes = "Test notları";
            var registrationDate = DateTime.UtcNow;

            // Act
            var attendee = new Attendee
            {
                Id = attendeeId,
                EventId = eventId,
                Name = name,
                Email = email,
                Phone = phone,
                Status = 0, // Onaylandı
                RegistrationDate = registrationDate,
                Notes = notes,
                HasAttended = true,
                IsCancelled = false,
                SendEmailNotification = true,
                TenantId = tenantId
            };

            // Assert
            Assert.Equal(attendeeId, attendee.Id);
            Assert.Equal(eventId, attendee.EventId);
            Assert.Equal(name, attendee.Name);
            Assert.Equal(email, attendee.Email);
            Assert.Equal(phone, attendee.Phone);
            Assert.Equal(0, attendee.Status);
            Assert.Equal(registrationDate, attendee.RegistrationDate);
            Assert.Equal(notes, attendee.Notes);
            Assert.True(attendee.HasAttended);
            Assert.False(attendee.IsCancelled);
            Assert.True(attendee.SendEmailNotification);
            Assert.Equal(tenantId, attendee.TenantId);
        }

        [Fact]
        public void Attendee_WithInvalidEmail_ThrowsException()
        {
            // Arrange & Act & Assert
            var invalidEmail = "invalid-email";

            var exception = Assert.Throws<ArgumentException>(() => new Attendee
            {
                EventId = Guid.NewGuid(),
                Name = "Test Katılımcı",
                Email = invalidEmail,
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("valid email address", exception.Message);
        }

        [Fact]
        public void Attendee_WithTooLongName_ThrowsException()
        {
            // Arrange & Act & Assert
            var longName = new string('A', 101); // 101 karakter (limit 100)

            var exception = Assert.Throws<ArgumentException>(() => new Attendee
            {
                EventId = Guid.NewGuid(),
                Name = longName,
                Email = "test@example.com",
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("must not exceed 100 characters", exception.Message);
        }

        [Fact]
        public void Attendee_WithTooLongNotes_ThrowsException()
        {
            // Arrange & Act & Assert
            var longNotes = new string('A', 501); // 501 karakter (limit 500)

            var exception = Assert.Throws<ArgumentException>(() => new Attendee
            {
                EventId = Guid.NewGuid(),
                Name = "Test Katılımcı",
                Email = "test@example.com",
                Notes = longNotes,
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("must not exceed 500 characters", exception.Message);
        }

        [Fact]
        public void Attendee_WithTooLongPhone_ThrowsException()
        {
            // Arrange & Act & Assert
            var longPhone = new string('1', 21); // 21 karakter (limit 20)

            var exception = Assert.Throws<ArgumentException>(() => new Attendee
            {
                EventId = Guid.NewGuid(),
                Name = "Test Katılımcı",
                Email = "test@example.com",
                Phone = longPhone,
                TenantId = Guid.NewGuid()
            });

            Assert.Contains("must not exceed 20 characters", exception.Message);
        }

        [Fact]
        public void Attendee_DefaultValues_SetCorrectly()
        {
            // Arrange & Act
            var attendee = new Attendee
            {
                EventId = Guid.NewGuid(),
                Name = "Test Katılımcı",
                Email = "test@example.com",
                TenantId = Guid.NewGuid()
            };

            // Assert
            Assert.Equal(0, attendee.Status); // default: Onaylandı
            Assert.False(attendee.HasAttended); // default: false
            Assert.False(attendee.IsCancelled); // default: false
            Assert.True(attendee.SendEmailNotification); // default: true
            Assert.True(DateTime.UtcNow >= attendee.RegistrationDate); // default: şimdiki zaman (veya daha öncesi)
            Assert.True((DateTime.UtcNow - attendee.RegistrationDate).TotalSeconds < 10); // son 10 saniye içinde
        }
    }
} 