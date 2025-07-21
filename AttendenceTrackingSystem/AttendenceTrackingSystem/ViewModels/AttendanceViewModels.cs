using System.ComponentModel.DataAnnotations;

namespace AttendanceTrackingSystem.ViewModels
{
    public class AttendanceCheckInViewModel
    {
        [Required]
        public int UserId { get; set; }
        public DateTime CheckInTime { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public bool IsLateCheckIn => CheckInTime.TimeOfDay > new TimeSpan(9, 0, 0); // After 9:00 AM
    }

    public class AttendanceCheckOutViewModel
    {
        [Required]
        public int AttendanceId { get; set; }
        public DateTime CheckOutTime { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public bool IsEarlyCheckOut => CheckOutTime.TimeOfDay < new TimeSpan(17, 30, 0); // Before 5:30 PM
    }

    public class AttendanceRecordViewModel
    {
        public int AttendanceId { get; set; }
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string? Notes { get; set; }
        public string? Location { get; set; }

        // Computed Properties
        public TimeSpan? WorkingHours => CheckOut.HasValue ? CheckOut.Value - CheckIn : null;
        public string WorkingHoursDisplay => WorkingHours.HasValue ?
            $"{WorkingHours.Value.Hours:00}:{WorkingHours.Value.Minutes:00}" : "--:--";
        public string StatusDisplay => CheckOut.HasValue ? "Hoàn thành" : "Đang làm việc";
        public string StatusBadgeClass => CheckOut.HasValue ? "bg-success" : "bg-primary";
        public bool CanCheckOut => !CheckOut.HasValue;
        public string DateDisplay => CheckIn.ToString("dd/MM/yyyy");
        public string CheckInDisplay => CheckIn.ToString("HH:mm");
        public string CheckOutDisplay => CheckOut?.ToString("HH:mm") ?? "--:--";

        // Working hours analysis
        public bool IsLateCheckIn => CheckIn.TimeOfDay > new TimeSpan(9, 0, 0);
        public bool IsEarlyCheckOut => CheckOut?.TimeOfDay < new TimeSpan(17, 30, 0);
        public bool IsFullDay => WorkingHours?.TotalHours >= 8;
        public string WorkingHoursStatus => GetWorkingHoursStatus();

        private string GetWorkingHoursStatus()
        {
            if (!WorkingHours.HasValue) return "Chưa hoàn thành";

            var hours = WorkingHours.Value.TotalHours;
            return hours switch
            {
                >= 8 => "Đủ giờ",
                >= 6 => "Thiếu giờ",
                _ => "Làm việc ngắn"
            };
        }
    }

    public class AttendanceReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";
        public List<AttendanceRecordViewModel> Records { get; set; } = new();

        // Statistics
        public int TotalWorkingDays => Records.Count(r => r.CheckOut.HasValue);
        public int TotalPresentDays => Records.Count;
        public TimeSpan TotalWorkingHours => Records
            .Where(r => r.WorkingHours.HasValue)
            .Aggregate(TimeSpan.Zero, (total, record) => total + record.WorkingHours!.Value);
        public double AverageWorkingHours => TotalWorkingDays > 0 ?
            TotalWorkingHours.TotalHours / TotalWorkingDays : 0;

        // Attendance quality metrics
        public int LateCheckIns => Records.Count(r => r.IsLateCheckIn);
        public int EarlyCheckOuts => Records.Count(r => r.IsEarlyCheckOut);
        public int FullWorkingDays => Records.Count(r => r.IsFullDay);
        public double AttendanceScore => CalculateAttendanceScore();

        private double CalculateAttendanceScore()
        {
            if (TotalPresentDays == 0) return 0;

            var score = 100.0;
            score -= (LateCheckIns * 5.0 / TotalPresentDays) * 100; // -5 points per late check-in
            score -= (EarlyCheckOuts * 3.0 / TotalPresentDays) * 100; // -3 points per early check-out

            return Math.Max(0, score);
        }
    }

    public class AttendanceFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? UserId { get; set; }
        public string? Department { get; set; }
        public string? Status { get; set; } // "Completed", "Active", "All"
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // String properties for form binding
        public string? FromDateStr
        {
            get => FromDate?.ToString("yyyy-MM-dd");
            set => FromDate = string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);
        }

        public string? ToDateStr
        {
            get => ToDate?.ToString("yyyy-MM-dd");
            set => ToDate = string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);
        }
    }

    public class TodayAttendanceStatusViewModel
    {
        public bool HasCheckedIn { get; set; }
        public bool HasCheckedOut { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? AttendanceId { get; set; }
        public TimeSpan? CurrentWorkingTime => HasCheckedIn && !HasCheckedOut ?
            DateTime.Now - CheckInTime!.Value : null;

        public string StatusMessage => GetStatusMessage();
        public string StatusBadgeClass => GetStatusBadgeClass();
        public bool CanCheckIn => !HasCheckedIn;
        public bool CanCheckOut => HasCheckedIn && !HasCheckedOut;

        private string GetStatusMessage()
        {
            if (!HasCheckedIn) return "Chưa chấm công";
            if (!HasCheckedOut) return "Đang làm việc";
            return "Đã hoàn thành";
        }

        private string GetStatusBadgeClass()
        {
            if (!HasCheckedIn) return "bg-warning text-dark";
            if (!HasCheckedOut) return "bg-primary";
            return "bg-success";
        }
    }
}
