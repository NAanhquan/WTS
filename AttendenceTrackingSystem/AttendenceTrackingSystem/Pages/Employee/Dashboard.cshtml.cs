using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceTrackingSystem.Pages.Employee
{
    public class DashboardModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IAttendanceService _attendanceService;

        public DashboardModel(
            IAuthenticationService authService,
            IAttendanceService attendanceService)
        {
            _authService = authService;
            _attendanceService = attendanceService;
        }

        public TodayAttendanceStatusViewModel TodayStatus { get; set; } = new();
        public AttendanceRecordViewModel? TodayAttendance { get; set; }
        public List<AttendanceRecordViewModel> RecentAttendance { get; set; } = new();
        public AttendanceReportViewModel WeeklyReport { get; set; } = new();

        [BindProperty]
        public AttendanceCheckInViewModel CheckInInput { get; set; } = new();

        [BindProperty]
        public AttendanceCheckOutViewModel CheckOutInput { get; set; } = new();

        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            await LoadDashboardDataAsync(currentUser.UserId);
            return Page();
        }

        public async Task<IActionResult> OnPostCheckInAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            CheckInInput.UserId = currentUser.UserId;
            var result = await _attendanceService.CheckInAsync(CheckInInput);

            Message = result.Message;
            IsSuccess = result.Success;

            await LoadDashboardDataAsync(currentUser.UserId);
            return Page();
        }

        public async Task<IActionResult> OnPostCheckOutAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var result = await _attendanceService.CheckOutAsync(CheckOutInput);

            Message = result.Message;
            IsSuccess = result.Success;

            await LoadDashboardDataAsync(currentUser.UserId);
            return Page();
        }

        private async Task LoadDashboardDataAsync(int userId)
        {
            // Load today's attendance status
            TodayStatus = await _attendanceService.GetTodayAttendanceStatusAsync(userId);
            TodayAttendance = await _attendanceService.GetTodayAttendanceAsync(userId);

            // Prepare check-out input if needed
            if (TodayStatus.AttendanceId.HasValue)
            {
                CheckOutInput.AttendanceId = TodayStatus.AttendanceId.Value;
            }

            // Load recent attendance (last 7 days)
            var weekAgo = DateTime.Today.AddDays(-7);
            RecentAttendance = await _attendanceService.GetUserAttendanceHistoryAsync(userId, weekAgo, DateTime.Today);

            // Generate weekly report
            WeeklyReport = await _attendanceService.GenerateUserAttendanceReportAsync(userId, weekAgo, DateTime.Today);
        }
    }
}
