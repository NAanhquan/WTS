using AttendanceTrackingSystem.ViewModels;

namespace AttendanceTrackingSystem.Services.Interfaces
{
    public interface ILeaveRequestService
    {
        // Employee functions
        Task<(bool Success, string Message)> CreateLeaveRequestAsync(LeaveRequestCreateViewModel model);
        Task<List<LeaveRequestViewModel>> GetUserLeaveRequestsAsync(int userId);
        Task<LeaveRequestViewModel?> GetLeaveRequestByIdAsync(int leaveRequestId);
        Task<(bool Success, string Message)> UpdateLeaveRequestAsync(LeaveRequestUpdateViewModel model);
        Task<(bool Success, string Message)> CancelLeaveRequestAsync(int leaveRequestId, int userId);
        Task<LeaveBalanceViewModel> GetUserLeaveBalanceAsync(int userId, int year);

        // Manager functions
        Task<List<LeaveRequestViewModel>> GetTeamLeaveRequestsAsync(int managerId);
        Task<List<LeaveRequestViewModel>> GetDepartmentLeaveRequestsAsync(string department);
        Task<(bool Success, string Message)> ApproveLeaveRequestAsync(LeaveRequestApprovalViewModel model);
        Task<(bool Success, string Message)> RejectLeaveRequestAsync(LeaveRequestApprovalViewModel model);

        // Admin functions
        Task<List<LeaveRequestViewModel>> GetAllLeaveRequestsAsync();
        Task<List<LeaveRequestViewModel>> GetFilteredLeaveRequestsAsync(LeaveRequestFilterViewModel filter);
        Task<LeaveRequestStatisticsViewModel> GetLeaveRequestStatisticsAsync();
        Task<List<LeaveRequestViewModel>> GetPendingLeaveRequestsAsync();
        Task<(bool Success, string Message)> DeleteLeaveRequestAsync(int leaveRequestId);

        // Utility functions
        Task<int> GetRemainingLeaveDaysAsync(int userId, int year, string leaveType = "Annual");
        Task<bool> HasConflictingLeaveAsync(int userId, DateOnly startDate, DateOnly endDate, int? excludeRequestId = null);
        Task<bool> IsValidLeaveRequestAsync(LeaveRequestCreateViewModel model);
        Task<List<string>> GetDepartmentsWithLeaveRequestsAsync();
        Task<Dictionary<string, int>> GetLeaveRequestCountByStatusAsync();

        // Reporting functions
        Task<List<LeaveRequestViewModel>> GetLeaveRequestsByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<List<LeaveRequestViewModel>> GetUpcomingLeaveRequestsAsync(int days = 7);
        Task<Dictionary<string, object>> GetLeaveRequestSummaryAsync(int? userId = null, int? year = null);
    }
}
