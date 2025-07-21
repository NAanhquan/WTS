using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceTrackingSystem.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly AttendanceTrackingSystemContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            AttendanceTrackingSystemContext context,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardStatisticsViewModel> GetAdminStatisticsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var statistics = new DashboardStatisticsViewModel
                {
                    TotalUsers = await GetTotalUsersAsync(),
                    TodayActiveUsers = await GetActiveUsersCountAsync(today),
                    PendingLeaveRequests = await GetPendingLeaveRequestsAsync(),
                    ApprovedLeaveRequests = await _context.LeaveRequests
                        .CountAsync(lr => lr.Status == "Approved"),
                    RejectedLeaveRequests = await _context.LeaveRequests
                        .CountAsync(lr => lr.Status == "Rejected"),
                    UnresolvedComplaints = await GetUnresolvedComplaintsAsync(),
                    ResolvedComplaints = await _context.Complaints
                        .CountAsync(c => c.Status == "Resolved" || c.Status == "Processed"),
                    TotalAttendanceToday = await _context.Attendances
                        .CountAsync(a => a.CheckIn.Value.Date == today),
                    DepartmentUserCount = await GetDepartmentUserCountAsync(),
                    MonthlyAttendanceCount = await GetMonthlyAttendanceCountAsync()
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin statistics");
                return new DashboardStatisticsViewModel();
            }
        }

        public async Task<DashboardStatisticsViewModel> GetManagerStatisticsAsync(int managerId)
        {
            try
            {
                var manager = await _context.Users.FindAsync(managerId);
                if (manager == null) return new DashboardStatisticsViewModel();

                var today = DateTime.Today;
                var teamUserIds = await _context.Users
                    .Where(u => u.Department == manager.Department && u.UserId != managerId)
                    .Select(u => u.UserId)
                    .ToListAsync();

                var statistics = new DashboardStatisticsViewModel
                {
                    TotalUsers = teamUserIds.Count,
                    TodayActiveUsers = await _context.Attendances
                        .CountAsync(a => teamUserIds.Contains(a.UserId) && a.CheckIn.Value.Date == today),
                    PendingLeaveRequests = await _context.LeaveRequests
                        .CountAsync(lr => teamUserIds.Contains(lr.UserId) && lr.Status == "Pending"),
                    UnresolvedComplaints = await _context.Complaints
                        .CountAsync(c => teamUserIds.Contains(c.UserId) && c.Status == "Pending")
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting manager statistics for user {ManagerId}", managerId);
                return new DashboardStatisticsViewModel();
            }
        }

        public async Task<DashboardStatisticsViewModel> GetEmployeeStatisticsAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var statistics = new DashboardStatisticsViewModel
                {
                    TodayActiveUsers = await _context.Attendances
                        .CountAsync(a => a.UserId == userId && a.CheckIn.Value.Date == today),
                    PendingLeaveRequests = await _context.LeaveRequests
                        .CountAsync(lr => lr.UserId == userId && lr.Status == "Pending"),
                    ApprovedLeaveRequests = await _context.LeaveRequests
                        .CountAsync(lr => lr.UserId == userId && lr.Status == "Approved"),
                    UnresolvedComplaints = await _context.Complaints
                        .CountAsync(c => c.UserId == userId && c.Status == "Pending")
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee statistics for user {UserId}", userId);
                return new DashboardStatisticsViewModel();
            }
        }

        public async Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(int limit = 10)
        {
            try
            {
                var activities = new List<RecentActivityViewModel>();

                // Recent Leave Requests
                var recentLeaves = await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .OrderByDescending(lr => lr.StartDate)
                    .Take(limit / 2)
                    .Select(lr => new RecentActivityViewModel
                    {
                        Type = "LeaveRequest",
                        Description = $"{lr.User!.Name} đã gửi đơn nghỉ phép từ {lr.StartDate:dd/MM} đến {lr.EndDate:dd/MM}",
                        Timestamp = DateTime.Now, // Should use actual created date if available
                        Status = lr.Status ?? "Pending",
                        UserId = lr.UserId,
                        UserName = lr.User.Name,
                        Department = lr.User.Department
                    })
                    .ToListAsync();

                activities.AddRange(recentLeaves);

                // Recent Complaints
                var recentComplaints = await _context.Complaints
                    .Include(c => c.User)
                    .Where(c => c.DateFiled.HasValue)
                    .OrderByDescending(c => c.DateFiled)
                    .Take(limit / 2)
                    .Select(c => new RecentActivityViewModel
                    {
                        Type = "Complaint",
                        Description = $"{c.User!.Name} đã gửi khiếu nại",
                        Timestamp = c.DateFiled.HasValue
                        ? c.DateFiled.Value.ToDateTime(TimeOnly.MinValue)
                        : DateTime.Now,
                        Status = c.Status ?? "Pending",
                        UserId = c.UserId,
                        UserName = c.User.Name,
                        Department = c.User.Department
                    })
                    .ToListAsync();

                activities.AddRange(recentComplaints);

                // Recent Attendance (Check-ins today)
                var today = DateTime.Today;
                var recentAttendances = await _context.Attendances
                    .Include(a => a.User)
                    .Where(a => a.CheckIn.Value.Date == today)
                    .OrderByDescending(a => a.CheckIn)
                    .Take(limit / 3)
                    .Select(a => new RecentActivityViewModel
                    {
                        Type = "Attendance",
                        Description = $"{a.User!.Name} đã chấm công vào lúc {a.CheckIn:HH:mm}",
                        Timestamp = a.CheckIn.Value,
                        Status = a.CheckOut.HasValue ? "Completed" : "Active",
                        UserId = a.UserId,
                        UserName = a.User.Name,
                        Department = a.User.Department
                    })
                    .ToListAsync();

                activities.AddRange(recentAttendances);

                return activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return new List<RecentActivityViewModel>();
            }
        }

        public async Task<List<RecentActivityViewModel>> GetTeamActivitiesAsync(int managerId, int limit = 10)
        {
            try
            {
                var manager = await _context.Users.FindAsync(managerId);
                if (manager == null) return new List<RecentActivityViewModel>();

                var teamUserIds = await _context.Users
                    .Where(u => u.Department == manager.Department && u.UserId != managerId)
                    .Select(u => u.UserId)
                    .ToListAsync();

                var activities = new List<RecentActivityViewModel>();

                // Team Leave Requests
                var teamLeaves = await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => teamUserIds.Contains(lr.UserId))
                    .OrderByDescending(lr => lr.StartDate)
                    .Take(limit)
                    .Select(lr => new RecentActivityViewModel
                    {
                        Type = "LeaveRequest",
                        Description = $"{lr.User!.Name} đã gửi đơn nghỉ phép",
                        Timestamp = DateTime.Now,
                        Status = lr.Status ?? "Pending",
                        UserId = lr.UserId,
                        UserName = lr.User.Name,
                        Department = lr.User.Department
                    })
                    .ToListAsync();

                activities.AddRange(teamLeaves);

                return activities.OrderByDescending(a => a.Timestamp).Take(limit).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team activities for manager {ManagerId}", managerId);
                return new List<RecentActivityViewModel>();
            }
        }

        public async Task<int> GetTotalUsersAsync()
        {
            try
            {
                return await _context.Users.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total users count");
                return 0;
            }
        }

        public async Task<int> GetActiveUsersCountAsync(DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                return await _context.Attendances
                    .Where(a => a.CheckIn.Value.Date == targetDate)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users count for date {Date}", date);
                return 0;
            }
        }

        public async Task<int> GetPendingLeaveRequestsAsync()
        {
            try
            {
                return await _context.LeaveRequests
                    .CountAsync(lr => lr.Status == "Pending");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending leave requests count");
                return 0;
            }
        }

        public async Task<int> GetUnresolvedComplaintsAsync()
        {
            try
            {
                return await _context.Complaints
                    .CountAsync(c => c.Status == "Pending");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unresolved complaints count");
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> GetDepartmentUserCountAsync()
        {
            try
            {
                return await _context.Users
                    .GroupBy(u => u.Department ?? "Unknown")
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department user count");
                return new Dictionary<string, int>();
            }
        }

        private async Task<Dictionary<string, int>> GetMonthlyAttendanceCountAsync()
        {
            try
            {
                var result = new Dictionary<string, int>();
                var currentMonth = DateTime.Today;

                for (int i = 0; i < 6; i++)
                {
                    var month = currentMonth.AddMonths(-i);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var count = await _context.Attendances
                        .CountAsync(a => a.CheckIn.Value.Date >= monthStart && a.CheckIn.Value.Date <= monthEnd);

                    result.Add(month.ToString("MM/yyyy"), count);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly attendance count");
                return new Dictionary<string, int>();
            }
        }
    }
}
