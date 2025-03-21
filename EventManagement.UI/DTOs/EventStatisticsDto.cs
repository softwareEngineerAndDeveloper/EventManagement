using System;

namespace EventManagement.UI.DTOs
{
    /// <summary>
    /// Etkinlik istatistiklerini içeren DTO
    /// </summary>
    public class EventStatisticsDto
    {
        /// <summary>
        /// Etkinlik ID
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Etkinlik başlığı
        /// </summary>
        public string EventTitle { get; set; } = string.Empty;

        /// <summary>
        /// Etkinlik kapasitesi
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Toplam katılımcı sayısı
        /// </summary>
        public int TotalAttendees { get; set; }

        /// <summary>
        /// Toplam kayıt sayısı
        /// </summary>
        public int TotalRegistrations { get; set; }

        /// <summary>
        /// Onaylanmış katılımcı sayısı
        /// </summary>
        public int ConfirmedAttendees { get; set; }

        /// <summary>
        /// Onaylanmış kayıt sayısı
        /// </summary>
        public int ConfirmedRegistrations { get; set; }

        /// <summary>
        /// İptal edilmiş katılımcı sayısı
        /// </summary>
        public int CancelledAttendees { get; set; }

        /// <summary>
        /// İptal edilmiş kayıt sayısı
        /// </summary>
        public int CancelledRegistrations { get; set; }

        /// <summary>
        /// Bekleme listesindeki katılımcı sayısı
        /// </summary>
        public int WaitingListAttendees { get; set; }

        /// <summary>
        /// Bekleme listesindeki kayıt sayısı
        /// </summary>
        public int WaitingRegistrations { get; set; }

        /// <summary>
        /// Bekleme listesindeki kayıt sayısı (eski isim uyumluluğu için)
        /// </summary>
        public int WaitingListRegistrations
        {
            get { return WaitingRegistrations; }
            set { WaitingRegistrations = value; }
        }

        /// <summary>
        /// Mevcut kapasite
        /// </summary>
        public int AvailableCapacity { get; set; }

        /// <summary>
        /// Mevcut koltuk sayısı
        /// </summary>
        public int AvailableSeats { get; set; }

        /// <summary>
        /// Maksimum kapasite (null olabilir)
        /// </summary>
        public int? MaxCapacity { get; set; }

        /// <summary>
        /// Katılan sayısı
        /// </summary>
        public int AttendedCount { get; set; }

        /// <summary>
        /// Katılım oranı
        /// </summary>
        public decimal AttendanceRate { get; set; }

        /// <summary>
        /// Doluluk oranı (%)
        /// </summary>
        public double FillRate { get; set; }
    }
}