using System;
using System.Collections.Generic;

namespace EventManagement.UI.Models.Report
{
    public class DashboardReportViewModel
    {
        public UpcomingEventsReportViewModel UpcomingEvents { get; set; }
        public EventStatisticsViewModel EventStatistics { get; set; }
    }

    public class UpcomingEventsReportViewModel
    {
        public List<UpcomingEventViewModel> Events { get; set; }
        public int TotalEvents { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    public class UpcomingEventViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RegistrationCount { get; set; }
        public int? MaxAttendees { get; set; }
        public int DaysUntilEvent { get; set; }
        public decimal CapacityPercentage => MaxAttendees.HasValue && MaxAttendees.Value > 0 
            ? Math.Min(100, (decimal)RegistrationCount / MaxAttendees.Value * 100) 
            : 0;
    }

    public class EventStatisticsViewModel
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; }
        public string TenantName { get; set; }
        public DateTime EventDate { get; set; }
        public int TotalRegistrations { get; set; }
        public int ConfirmedAttendees { get; set; }
        public int CancelledRegistrations { get; set; }
        public int PendingRegistrations { get; set; }
        public int ActualAttendees { get; set; }
        public decimal AttendanceRate => TotalRegistrations > 0 
            ? Math.Round((decimal)ActualAttendees / TotalRegistrations * 100, 1)
            : 0;
        public decimal ConfirmationRate => TotalRegistrations > 0
            ? Math.Round((decimal)ConfirmedAttendees / TotalRegistrations * 100, 1)
            : 0;
        public decimal CancellationRate => TotalRegistrations > 0
            ? Math.Round((decimal)CancelledRegistrations / TotalRegistrations * 100, 1)
            : 0;
        public List<RegistrationsByDayViewModel> RegistrationsByDay { get; set; } = new List<RegistrationsByDayViewModel>();
        public List<RegistrationsByStatusViewModel> RegistrationsByStatus { get; set; } = new List<RegistrationsByStatusViewModel>();
    }

    public class RegistrationsByDayViewModel
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class RegistrationsByStatusViewModel
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
    }
} 