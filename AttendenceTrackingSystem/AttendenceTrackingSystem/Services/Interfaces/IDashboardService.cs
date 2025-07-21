using AttendanceTrackingSystem.Pages.Admin;
using AttendanceTrackingSystem.ViewModels;

namespace AttendanceTrackingSystem.Services.Interfaces
{
    public interface IDashboardService
    {
        // Admin Dashboard Statistics
        Task<DashboardStatisticsViewModel> GetAdminStatisticsAsync();
        Task<List<ViewModels.RecentActivityViewModel>> GetRecentActivitiesAsync(int limit = 10);

        // Manager Dashboard Statistics
        Task<DashboardStatisticsViewModel> GetManagerStatisticsAsync(int managerId);
        Task<List<ViewModels.RecentActivityViewModel>> GetTeamActivitiesAsync(int managerId, int limit = 10);

        // Employee Dashboard Statistics
        Task<DashboardStatisticsViewModel> GetEmployeeStatisticsAsync(int userId);

        // Common statistics methods
        Task<int> GetTotalUsersAsync();
        Task<int> GetActiveUsersCountAsync(DateTime? date = null);
        Task<int> GetPendingLeaveRequestsAsync();
        Task<int> GetUnresolvedComplaintsAsync();
        Task<Dictionary<string, int>> GetDepartmentUserCountAsync();
    }
}
