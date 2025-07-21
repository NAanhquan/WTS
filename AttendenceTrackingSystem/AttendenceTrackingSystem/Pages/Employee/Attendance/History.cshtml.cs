using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceTrackingSystem.Pages.Employee.Attendance
{
    public class HistoryModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IAttendanceService _attendanceService;

        public HistoryModel(
            IAuthenticationService authService,
            IAttendanceService attendanceService)
        {
            _authService = authService;
            _attendanceService = attendanceService;
        }

        public List<AttendanceRecordViewModel> AttendanceRecords { get; set; } = new();
        public AttendanceReportViewModel Report { get; set; } = new();

        [BindProperty]
        public AttendanceFilterViewModel Filter { get; set; } = new()
        {
            FromDate = DateTime.Today.AddMonths(-1),
            ToDate = DateTime.Today
        };

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            await LoadAttendanceDataAsync(currentUser.UserId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            await LoadAttendanceDataAsync(currentUser.UserId);
            return Page();
        }

        private async Task LoadAttendanceDataAsync(int userId)
        {
            var fromDate = Filter.FromDate ?? DateTime.Today.AddMonths(-1);
            var toDate = Filter.ToDate ?? DateTime.Today;

            AttendanceRecords = await _attendanceService.GetUserAttendanceHistoryAsync(userId, fromDate, toDate);
            Report = await _attendanceService.GenerateUserAttendanceReportAsync(userId, fromDate, toDate);
        }
    }
}
