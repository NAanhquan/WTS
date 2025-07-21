using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceTrackingSystem.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IDashboardService _dashboardService;

        public DashboardModel(
            IAuthenticationService authService,
            IDashboardService dashboardService)
        {
            _authService = authService;
            _dashboardService = dashboardService;
        }

        public DashboardStatisticsViewModel Statistics { get; set; } = new();
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();

        // Convenience properties for backward compatibility
        public int TotalUsers => Statistics.TotalUsers;
        public int TodayActiveUsers => Statistics.TodayActiveUsers;
        public int PendingLeaveRequests => Statistics.PendingLeaveRequests;
        public int UnresolvedComplaints => Statistics.UnresolvedComplaints;

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            if (!currentUser.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Employee/Dashboard");
            }

            try
            {
                // Load real data from database
                Statistics = await _dashboardService.GetAdminStatisticsAsync();
                RecentActivities = await _dashboardService.GetRecentActivitiesAsync(15);
            }
            catch (Exception ex)
            {
                // Log error và fallback to empty data
                Statistics = new DashboardStatisticsViewModel();
                RecentActivities = new List<RecentActivityViewModel>();

                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu dashboard.";
            }

            return Page();
        }
    }
}
