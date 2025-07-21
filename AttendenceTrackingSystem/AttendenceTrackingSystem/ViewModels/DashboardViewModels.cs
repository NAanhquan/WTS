namespace AttendanceTrackingSystem.ViewModels
{
    public class DashboardStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalActiveUsers { get; set; }
        public int TodayActiveUsers { get; set; }
        public int PendingLeaveRequests { get; set; }
        public int ApprovedLeaveRequests { get; set; }
        public int RejectedLeaveRequests { get; set; }
        public int UnresolvedComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int TotalAttendanceToday { get; set; }
        public Dictionary<string, int> DepartmentUserCount { get; set; } = new();
        public Dictionary<string, int> MonthlyAttendanceCount { get; set; } = new();

        // Calculated properties
        public double AttendanceRate => TotalUsers > 0 ? (double)TodayActiveUsers / TotalUsers * 100 : 0;
        public int TotalLeaveRequests => PendingLeaveRequests + ApprovedLeaveRequests + RejectedLeaveRequests;
        public int TotalComplaints => UnresolvedComplaints + ResolvedComplaints;
    }

    public class RecentActivityViewModel
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "";
        public int? UserId { get; set; }
        public string? UserName { get; set; } = "";
        public string? Department { get; set; } = "";

        // Display properties
        public string TimeDisplay => GetTimeDisplay();
        public string StatusBadgeClass => GetStatusBadgeClass();
        public string TypeIcon => GetTypeIcon();

        private string GetTimeDisplay()
        {
            var diff = DateTime.Now - Timestamp;

            return diff.TotalMinutes switch
            {
                < 1 => "Vừa xong",
                < 60 => $"{(int)diff.TotalMinutes} phút trước",
                < 1440 => $"{(int)diff.TotalHours} giờ trước",
                < 43200 => $"{(int)diff.TotalDays} ngày trước",
                _ => Timestamp.ToString("dd/MM/yyyy")
            };
        }

        private string GetStatusBadgeClass()
        {
            return Status.ToLower() switch
            {
                "pending" => "bg-warning text-dark",
                "approved" => "bg-success",
                "rejected" => "bg-danger",
                "resolved" => "bg-info",
                "completed" => "bg-success",
                _ => "bg-secondary"
            };
        }

        private string GetTypeIcon()
        {
            return Type.ToLower() switch
            {
                "leaverequest" => "bi-calendar-event",
                "complaint" => "bi-exclamation-diamond",
                "attendance" => "bi-clock",
                "user" => "bi-person-plus",
                _ => "bi-info-circle"
            };
        }
    }
}
