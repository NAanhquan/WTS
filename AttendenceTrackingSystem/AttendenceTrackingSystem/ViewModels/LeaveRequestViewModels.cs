using System.ComponentModel.DataAnnotations;

namespace AttendanceTrackingSystem.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int LeaveRequestId { get; set; }
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string LoginName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Computed Properties
        public string StatusDisplay => GetStatusDisplay();
        public string StatusBadgeClass => GetStatusBadgeClass();
        public string DateRangeDisplay => $"{StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}";
        public int TotalDays => (EndDate.DayNumber - StartDate.DayNumber) + 1;
        public bool CanEdit => Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
        public bool CanCancel => Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
        public bool CanApprove => Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
        public bool CanReject => Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
        public string CreatedDateDisplay => CreatedDate.ToString("dd/MM/yyyy HH:mm");

        private string GetStatusDisplay()
        {
            return Status.ToLower() switch
            {
                "pending" => "Chờ duyệt",
                "approved" => "Đã duyệt",
                "rejected" => "Đã từ chối",
                "cancelled" => "Đã hủy",
                _ => Status
            };
        }

        private string GetStatusBadgeClass()
        {
            return Status.ToLower() switch
            {
                "pending" => "bg-warning text-dark",
                "approved" => "bg-success",
                "rejected" => "bg-danger",
                "cancelled" => "bg-secondary",
                _ => "bg-secondary"
            };
        }
    }

    public class LeaveRequestCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [DataType(DataType.Date)]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [DataType(DataType.Date)]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [Required(ErrorMessage = "Loại nghỉ phép là bắt buộc")]
        public string LeaveType { get; set; } = "";

        [Required(ErrorMessage = "Lý do nghỉ phép là bắt buộc")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public string Reason { get; set; } = "";

        public int UserId { get; set; }

        // Computed Properties
        public int TotalDays => (EndDate.DayNumber - StartDate.DayNumber) + 1;

        // String properties for form binding
        public string StartDateStr
        {
            get => StartDate.ToString("yyyy-MM-dd");
            set => StartDate = string.IsNullOrEmpty(value) ? DateOnly.FromDateTime(DateTime.Today) : DateOnly.Parse(value);
        }

        public string EndDateStr
        {
            get => EndDate.ToString("yyyy-MM-dd");
            set => EndDate = string.IsNullOrEmpty(value) ? DateOnly.FromDateTime(DateTime.Today) : DateOnly.Parse(value);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu",
                    new[] { nameof(EndDate) });
            }

            if (StartDate < DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult(
                    "Không thể tạo đơn nghỉ phép cho ngày trong quá khứ",
                    new[] { nameof(StartDate) });
            }

            if (TotalDays > 30)
            {
                yield return new ValidationResult(
                    "Thời gian nghỉ phép không được vượt quá 30 ngày liên tiếp",
                    new[] { nameof(EndDate) });
            }

            // Validate leave type
            var validLeaveTypes = new[] { "Annual", "Sick", "Personal", "Maternity", "Emergency" };
            if (!validLeaveTypes.Contains(LeaveType))
            {
                yield return new ValidationResult(
                    "Loại nghỉ phép không hợp lệ",
                    new[] { nameof(LeaveType) });
            }
        }
    }

    public class LeaveRequestUpdateViewModel : IValidatableObject
    {
        [Required]
        public int LeaveRequestId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [DataType(DataType.Date)]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [DataType(DataType.Date)]
        public DateOnly EndDate { get; set; }

        [Required(ErrorMessage = "Loại nghỉ phép là bắt buộc")]
        public string LeaveType { get; set; } = "";

        [Required(ErrorMessage = "Lý do nghỉ phép là bắt buộc")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public string Reason { get; set; } = "";

        public int UserId { get; set; }
        public int TotalDays => (EndDate.DayNumber - StartDate.DayNumber) + 1;

        // String properties for form binding
        public string StartDateStr
        {
            get => StartDate.ToString("yyyy-MM-dd");
            set => StartDate = string.IsNullOrEmpty(value) ? DateOnly.FromDateTime(DateTime.Today) : DateOnly.Parse(value);
        }

        public string EndDateStr
        {
            get => EndDate.ToString("yyyy-MM-dd");
            set => EndDate = string.IsNullOrEmpty(value) ? DateOnly.FromDateTime(DateTime.Today) : DateOnly.Parse(value);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu",
                    new[] { nameof(EndDate) });
            }

            if (StartDate < DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult(
                    "Không thể sửa đơn nghỉ phép cho ngày trong quá khứ",
                    new[] { nameof(StartDate) });
            }

            if (TotalDays > 30)
            {
                yield return new ValidationResult(
                    "Thời gian nghỉ phép không được vượt quá 30 ngày liên tiếp",
                    new[] { nameof(EndDate) });
            }
        }
    }

    public class LeaveRequestApprovalViewModel
    {
        [Required]
        public int LeaveRequestId { get; set; }

        [Required(ErrorMessage = "Trạng thái phê duyệt là bắt buộc")]
        public string Status { get; set; } = ""; // "Approved" or "Rejected"

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? ApprovalNotes { get; set; }

        public int ApprovedBy { get; set; }
        public DateTime ApprovalDate { get; set; } = DateTime.Now;

        // For display purposes
        public string EmployeeName { get; set; } = "";
        public string DateRange { get; set; } = "";
        public string Reason { get; set; } = "";
        public int TotalDays { get; set; }
    }

    public class LeaveRequestFilterViewModel
    {
        public string? Status { get; set; }
        public string? Department { get; set; }
        public string? LeaveType { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int? UserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // String properties for form binding
        public string? FromDateStr
        {
            get => FromDate?.ToString("yyyy-MM-dd");
            set => FromDate = string.IsNullOrEmpty(value) ? null : DateOnly.Parse(value);
        }

        public string? ToDateStr
        {
            get => ToDate?.ToString("yyyy-MM-dd");
            set => ToDate = string.IsNullOrEmpty(value) ? null : DateOnly.Parse(value);
        }
    }

    public class LeaveRequestStatisticsViewModel
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int CancelledRequests { get; set; }
        public Dictionary<string, int> RequestsByMonth { get; set; } = new();
        public Dictionary<string, int> RequestsByDepartment { get; set; } = new();
        public Dictionary<string, int> RequestsByLeaveType { get; set; } = new();

        // Calculated properties
        public double ApprovalRate => TotalRequests > 0 ? (double)ApprovedRequests / TotalRequests * 100 : 0;
        public double RejectionRate => TotalRequests > 0 ? (double)RejectedRequests / TotalRequests * 100 : 0;
    }

    public class LeaveBalanceViewModel
    {
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = "";
        public int Year { get; set; } = DateTime.Now.Year;
        public int TotalAnnualLeave { get; set; } = 12; // Default 12 days per year
        public int UsedAnnualLeave { get; set; }
        public int RemainingAnnualLeave => TotalAnnualLeave - UsedAnnualLeave;
        public int TotalSickLeave { get; set; } = 30; // Default 30 days per year
        public int UsedSickLeave { get; set; }
        public int RemainingSickLeave => TotalSickLeave - UsedSickLeave;
        public List<LeaveRequestViewModel> RecentRequests { get; set; } = new();
    }

    // Enum for Leave Types
    public static class LeaveTypes
    {
        public const string Annual = "Annual";
        public const string Sick = "Sick";
        public const string Personal = "Personal";
        public const string Maternity = "Maternity";
        public const string Emergency = "Emergency";

        public static readonly Dictionary<string, string> DisplayNames = new()
        {
            { Annual, "Nghỉ phép năm" },
            { Sick, "Nghỉ ốm" },
            { Personal, "Nghỉ cá nhân" },
            { Maternity, "Nghỉ thai sản" },
            { Emergency, "Nghỉ khẩn cấp" }
        };

        public static List<(string Value, string Display)> GetAll()
        {
            return DisplayNames.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }
    }
}
