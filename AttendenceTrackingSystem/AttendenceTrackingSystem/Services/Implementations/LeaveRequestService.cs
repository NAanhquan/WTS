using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceTrackingSystem.Services.Implementations
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly AttendanceTrackingSystemContext _context;
        private readonly ILogger<LeaveRequestService> _logger;

        public LeaveRequestService(
            AttendanceTrackingSystemContext context,
            ILogger<LeaveRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Employee Functions

        public async Task<(bool Success, string Message)> CreateLeaveRequestAsync(LeaveRequestCreateViewModel model)
        {
            try
            {
                // Validate the request
                var validationResult = await IsValidLeaveRequestAsync(model);
                if (!validationResult)
                {
                    return (false, "Dữ liệu đơn nghỉ phép không hợp lệ.");
                }

                // Check for conflicting leave requests
                var hasConflict = await HasConflictingLeaveAsync(model.UserId, model.StartDate, model.EndDate);
                if (hasConflict)
                {
                    return (false, "Bạn đã có đơn nghỉ phép được duyệt trong khoảng thời gian này.");
                }

                // Check remaining leave days
                var remainingDays = await GetRemainingLeaveDaysAsync(model.UserId, model.StartDate.Year, model.LeaveType);
                if (model.LeaveType == LeaveTypes.Annual && model.TotalDays > remainingDays)
                {
                    return (false, $"Bạn chỉ còn {remainingDays} ngày nghỉ phép năm. Không thể tạo đơn nghỉ {model.TotalDays} ngày.");
                }

                // Create leave request
                var leaveRequest = new LeaveRequest
                {
                    UserId = model.UserId,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Reason = model.Reason,
                    Status = "Pending"
                    // Note: LeaveType field doesn't exist in current model, might need to add to database
                };

                _context.LeaveRequests.Add(leaveRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Leave request created for user {UserId} from {StartDate} to {EndDate}",
                    model.UserId, model.StartDate, model.EndDate);

                return (true, $"Đã tạo đơn nghỉ phép thành công từ {model.StartDate:dd/MM/yyyy} đến {model.EndDate:dd/MM/yyyy} ({model.TotalDays} ngày).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave request for user {UserId}", model.UserId);
                return (false, "Có lỗi xảy ra khi tạo đơn nghỉ phép. Vui lòng thử lại.");
            }
        }

        public async Task<List<LeaveRequestViewModel>> GetUserLeaveRequestsAsync(int userId)
        {
            try
            {
                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.UserId == userId)
                    .OrderByDescending(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave requests for user {UserId}", userId);
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<LeaveRequestViewModel?> GetLeaveRequestByIdAsync(int leaveRequestId)
        {
            try
            {
                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.LeaveRequestId == leaveRequestId)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave request {LeaveRequestId}", leaveRequestId);
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateLeaveRequestAsync(LeaveRequestUpdateViewModel model)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests.FindAsync(model.LeaveRequestId);
                if (leaveRequest == null)
                {
                    return (false, "Không tìm thấy đơn nghỉ phép.");
                }

                // Check if user owns this request
                if (leaveRequest.UserId != model.UserId)
                {
                    return (false, "Bạn không có quyền sửa đơn nghỉ phép này.");
                }

                // Check if request can be edited
                if (leaveRequest.Status?.ToLower() != "pending")
                {
                    return (false, "Chỉ có thể sửa đơn nghỉ phép đang chờ duyệt.");
                }

                // Check for conflicting leave requests (excluding current one)
                var hasConflict = await HasConflictingLeaveAsync(model.UserId, model.StartDate, model.EndDate, model.LeaveRequestId);
                if (hasConflict)
                {
                    return (false, "Thời gian nghỉ phép bị trung lặp với đơn nghỉ phép khác đã được duyệt.");
                }

                // Update the request
                leaveRequest.StartDate = model.StartDate;
                leaveRequest.EndDate = model.EndDate;
                leaveRequest.Reason = model.Reason;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Leave request {LeaveRequestId} updated by user {UserId}",
                    model.LeaveRequestId, model.UserId);

                return (true, "Cập nhật đơn nghỉ phép thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating leave request {LeaveRequestId}", model.LeaveRequestId);
                return (false, "Có lỗi xảy ra khi cập nhật đơn nghỉ phép.");
            }
        }

        public async Task<(bool Success, string Message)> CancelLeaveRequestAsync(int leaveRequestId, int userId)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == leaveRequestId);

                if (leaveRequest == null)
                {
                    return (false, "Không tìm thấy đơn nghỉ phép.");
                }

                // Check ownership
                if (leaveRequest.UserId != userId)
                {
                    return (false, "Bạn không có quyền hủy đơn nghỉ phép này.");
                }

                // Check if can be cancelled
                if (leaveRequest.Status?.ToLower() == "approved" && leaveRequest.StartDate <= DateOnly.FromDateTime(DateTime.Today))
                {
                    return (false, "Không thể hủy đơn nghỉ phép đã được duyệt và đã bắt đầu.");
                }

                if (leaveRequest.Status?.ToLower() == "cancelled")
                {
                    return (false, "Đơn nghỉ phép này đã được hủy trước đó.");
                }

                leaveRequest.Status = "Cancelled";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Leave request {LeaveRequestId} cancelled by user {UserId}",
                    leaveRequestId, userId);

                return (true, "Đã hủy đơn nghỉ phép thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling leave request {LeaveRequestId}", leaveRequestId);
                return (false, "Có lỗi xảy ra khi hủy đơn nghỉ phép.");
            }
        }

        public async Task<LeaveBalanceViewModel> GetUserLeaveBalanceAsync(int userId, int year)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return new LeaveBalanceViewModel { UserId = userId, Year = year };
                }

                // Calculate used leave days for the year
                var usedAnnualLeave = await _context.LeaveRequests
                    .Where(lr => lr.UserId == userId &&
                                lr.StartDate.HasValue &&
                                lr.StartDate.Value.Year == year &&
                                lr.Status == "Approved")
                    .SumAsync(lr => lr.StartDate.HasValue && lr.EndDate.HasValue ?
                        (lr.EndDate.Value.DayNumber - lr.StartDate.Value.DayNumber) + 1 : 0);

                var recentRequests = await GetUserLeaveRequestsAsync(userId);

                return new LeaveBalanceViewModel
                {
                    UserId = userId,
                    EmployeeName = user.Name ?? "",
                    Year = year,
                    UsedAnnualLeave = usedAnnualLeave,
                    RecentRequests = recentRequests.Take(5).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave balance for user {UserId}", userId);
                return new LeaveBalanceViewModel { UserId = userId, Year = year };
            }
        }

        #endregion

        #region Manager Functions

        public async Task<List<LeaveRequestViewModel>> GetTeamLeaveRequestsAsync(int managerId)
        {
            try
            {
                // Get manager's department
                var manager = await _context.Users.FindAsync(managerId);
                if (manager == null) return new List<LeaveRequestViewModel>();

                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.User!.Department == manager.Department && lr.UserId != managerId)
                    .OrderByDescending(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team leave requests for manager {ManagerId}", managerId);
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<List<LeaveRequestViewModel>> GetDepartmentLeaveRequestsAsync(string department)
        {
            try
            {
                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.User!.Department == department)
                    .OrderByDescending(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department leave requests for {Department}", department);
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<(bool Success, string Message)> ApproveLeaveRequestAsync(LeaveRequestApprovalViewModel model)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == model.LeaveRequestId);

                if (leaveRequest == null)
                {
                    return (false, "Không tìm thấy đơn nghỉ phép.");
                }

                if (leaveRequest.Status?.ToLower() != "pending")
                {
                    return (false, "Chỉ có thể duyệt đơn nghỉ phép đang chờ xét duyệt.");
                }

                // Double-check for conflicts before approval
                var hasConflict = await HasConflictingLeaveAsync(
                    leaveRequest.UserId,
                    leaveRequest.StartDate ?? DateOnly.MinValue,
                    leaveRequest.EndDate ?? DateOnly.MinValue,
                    model.LeaveRequestId);

                if (hasConflict)
                {
                    return (false, "Không thể duyệt do trung lặp với đơn nghỉ phép khác đã được duyệt.");
                }

                leaveRequest.Status = "Approved";
                // Note: ApprovedBy and ApprovedDate fields don't exist in current model
                // You might want to add these fields to the database

                await _context.SaveChangesAsync();

                // TODO: Send notification to employee
                // await _notificationService.SendLeaveApprovalNotification(leaveRequest);

                _logger.LogInformation("Leave request {LeaveRequestId} approved by {ApproverId}",
                    model.LeaveRequestId, model.ApprovedBy);

                return (true, $"Đã duyệt đơn nghỉ phép của {leaveRequest.User?.Name} thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving leave request {LeaveRequestId}", model.LeaveRequestId);
                return (false, "Có lỗi xảy ra khi duyệt đơn nghỉ phép.");
            }
        }

        public async Task<(bool Success, string Message)> RejectLeaveRequestAsync(LeaveRequestApprovalViewModel model)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == model.LeaveRequestId);

                if (leaveRequest == null)
                {
                    return (false, "Không tìm thấy đơn nghỉ phép.");
                }

                if (leaveRequest.Status?.ToLower() != "pending")
                {
                    return (false, "Chỉ có thể từ chối đơn nghỉ phép đang chờ xét duyệt.");
                }

                leaveRequest.Status = "Rejected";

                await _context.SaveChangesAsync();

                // TODO: Send notification to employee with rejection reason
                // await _notificationService.SendLeaveRejectionNotification(leaveRequest, model.ApprovalNotes);

                _logger.LogInformation("Leave request {LeaveRequestId} rejected by {ApproverId}. Reason: {Reason}",
                    model.LeaveRequestId, model.ApprovedBy, model.ApprovalNotes);

                return (true, $"Đã từ chối đơn nghỉ phép của {leaveRequest.User?.Name}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting leave request {LeaveRequestId}", model.LeaveRequestId);
                return (false, "Có lỗi xảy ra khi từ chối đơn nghỉ phép.");
            }
        }

        #endregion

        #region Admin Functions

        public async Task<List<LeaveRequestViewModel>> GetAllLeaveRequestsAsync()
        {
            try
            {
                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .OrderByDescending(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all leave requests");
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<List<LeaveRequestViewModel>> GetFilteredLeaveRequestsAsync(LeaveRequestFilterViewModel filter)
        {
            try
            {
                var query = _context.LeaveRequests
                    .Include(lr => lr.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    query = query.Where(lr => lr.Status == filter.Status);
                }

                if (!string.IsNullOrEmpty(filter.Department))
                {
                    query = query.Where(lr => lr.User!.Department == filter.Department);
                }

                if (filter.UserId.HasValue)
                {
                    query = query.Where(lr => lr.UserId == filter.UserId.Value);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(lr => lr.StartDate >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(lr => lr.EndDate <= filter.ToDate.Value);
                }

                return await query
                    .OrderByDescending(lr => lr.StartDate)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered leave requests");
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<List<LeaveRequestViewModel>> GetPendingLeaveRequestsAsync()
        {
            try
            {
                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.Status == "Pending")
                    .OrderBy(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending leave requests");
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<LeaveRequestStatisticsViewModel> GetLeaveRequestStatisticsAsync()
        {
            try
            {
                var totalRequests = await _context.LeaveRequests.CountAsync();
                var pendingRequests = await _context.LeaveRequests.CountAsync(lr => lr.Status == "Pending");
                var approvedRequests = await _context.LeaveRequests.CountAsync(lr => lr.Status == "Approved");
                var rejectedRequests = await _context.LeaveRequests.CountAsync(lr => lr.Status == "Rejected");
                var cancelledRequests = await _context.LeaveRequests.CountAsync(lr => lr.Status == "Cancelled");

                var requestsByDepartment = await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .GroupBy(lr => lr.User!.Department ?? "Unknown")
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                return new LeaveRequestStatisticsViewModel
                {
                    TotalRequests = totalRequests,
                    PendingRequests = pendingRequests,
                    ApprovedRequests = approvedRequests,
                    RejectedRequests = rejectedRequests,
                    CancelledRequests = cancelledRequests,
                    RequestsByDepartment = requestsByDepartment
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave request statistics");
                return new LeaveRequestStatisticsViewModel();
            }
        }

        public async Task<(bool Success, string Message)> DeleteLeaveRequestAsync(int leaveRequestId)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .FirstOrDefaultAsync(lr => lr.LeaveRequestId == leaveRequestId);

                if (leaveRequest == null)
                {
                    return (false, "Không tìm thấy đơn nghỉ phép.");
                }

                // Only allow deletion if not approved and in the future
                if (leaveRequest.Status == "Approved" && leaveRequest.StartDate <= DateOnly.FromDateTime(DateTime.Today))
                {
                    return (false, "Không thể xóa đơn nghỉ phép đã được duyệt và đã bắt đầu.");
                }

                _context.LeaveRequests.Remove(leaveRequest);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Leave request {LeaveRequestId} deleted for user {UserName}",
                    leaveRequestId, leaveRequest.User?.Name);

                return (true, $"Đã xóa đơn nghỉ phép của {leaveRequest.User?.Name} thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting leave request {LeaveRequestId}", leaveRequestId);
                return (false, "Có lỗi xảy ra khi xóa đơn nghỉ phép.");
            }
        }

        #endregion

        #region Utility Functions

        public async Task<int> GetRemainingLeaveDaysAsync(int userId, int year, string leaveType = "Annual")
        {
            try
            {
                // Default annual leave allowance (this could be configurable per user/role)
                var totalAllowance = leaveType switch
                {
                    "Annual" => 12,
                    "Sick" => 30,
                    _ => 12
                };

                var usedDays = await _context.LeaveRequests
                    .Where(lr => lr.UserId == userId &&
                                lr.StartDate.HasValue &&
                                lr.StartDate.Value.Year == year &&
                                lr.Status == "Approved")
                    .SumAsync(lr => lr.StartDate.HasValue && lr.EndDate.HasValue ?
                        (lr.EndDate.Value.DayNumber - lr.StartDate.Value.DayNumber) + 1 : 0);

                return Math.Max(0, totalAllowance - usedDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting remaining leave days for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> HasConflictingLeaveAsync(int userId, DateOnly startDate, DateOnly endDate, int? excludeRequestId = null)
        {
            try
            {
                var query = _context.LeaveRequests
                    .Where(lr => lr.UserId == userId &&
                                lr.Status == "Approved" &&
                                lr.StartDate.HasValue &&
                                lr.EndDate.HasValue);

                if (excludeRequestId.HasValue)
                {
                    query = query.Where(lr => lr.LeaveRequestId != excludeRequestId.Value);
                }

                return await query.AnyAsync(lr =>
                    (startDate >= lr.StartDate && startDate <= lr.EndDate) ||
                    (endDate >= lr.StartDate && endDate <= lr.EndDate) ||
                    (startDate <= lr.StartDate && endDate >= lr.EndDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conflicting leave for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsValidLeaveRequestAsync(LeaveRequestCreateViewModel model)
        {
            try
            {
                // Basic validation
                if (model.StartDate < DateOnly.FromDateTime(DateTime.Today))
                    return false;

                if (model.EndDate < model.StartDate)
                    return false;

                if (model.TotalDays > 30)
                    return false;

                if (string.IsNullOrWhiteSpace(model.Reason))
                    return false;

                // Check if user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == model.UserId);
                if (!userExists)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating leave request");
                return false;
            }
        }

        public async Task<List<string>> GetDepartmentsWithLeaveRequestsAsync()
        {
            try
            {
                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Select(lr => lr.User!.Department ?? "Unknown")
                    .Distinct()
                    .Where(d => !string.IsNullOrEmpty(d))
                    .OrderBy(d => d)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments with leave requests");
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, int>> GetLeaveRequestCountByStatusAsync()
        {
            try
            {
                return await _context.LeaveRequests
                    .GroupBy(lr => lr.Status ?? "Unknown")
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave request count by status");
                return new Dictionary<string, int>();
            }
        }

        public async Task<List<LeaveRequestViewModel>> GetLeaveRequestsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var fromDateOnly = DateOnly.FromDateTime(fromDate);
                var toDateOnly = DateOnly.FromDateTime(toDate);

                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.StartDate >= fromDateOnly && lr.EndDate <= toDateOnly)
                    .OrderBy(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave requests by date range");
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<List<LeaveRequestViewModel>> GetUpcomingLeaveRequestsAsync(int days = 7)
        {
            try
            {
                var fromDate = DateOnly.FromDateTime(DateTime.Today);
                var toDate = DateOnly.FromDateTime(DateTime.Today.AddDays(days));

                return await _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.Status == "Approved" &&
                                lr.StartDate >= fromDate &&
                                lr.StartDate <= toDate)
                    .OrderBy(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestViewModel
                    {
                        LeaveRequestId = lr.LeaveRequestId,
                        UserId = lr.UserId,
                        EmployeeName = lr.User!.Name ?? "",
                        LoginName = lr.User.Username ?? "",
                        Department = lr.User.Department ?? "",
                        Position = lr.User.Position ?? "",
                        StartDate = lr.StartDate ?? DateOnly.MinValue,
                        EndDate = lr.EndDate ?? DateOnly.MinValue,
                        Reason = lr.Reason ?? "",
                        Status = lr.Status ?? "Pending"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming leave requests");
                return new List<LeaveRequestViewModel>();
            }
        }

        public async Task<Dictionary<string, object>> GetLeaveRequestSummaryAsync(int? userId = null, int? year = null)
        {
            try
            {
                var currentYear = year ?? DateTime.Now.Year;
                var summary = new Dictionary<string, object>();

                var query = _context.LeaveRequests
                    .Include(lr => lr.User)
                    .Where(lr => lr.StartDate.HasValue && lr.StartDate.Value.Year == currentYear);

                if (userId.HasValue)
                {
                    query = query.Where(lr => lr.UserId == userId.Value);
                }

                var totalRequests = await query.CountAsync();
                var approvedRequests = await query.CountAsync(lr => lr.Status == "Approved");
                var pendingRequests = await query.CountAsync(lr => lr.Status == "Pending");

                var totalApprovedDays = await query
                    .Where(lr => lr.Status == "Approved")
                    .SumAsync(lr => lr.StartDate.HasValue && lr.EndDate.HasValue ?
                        (lr.EndDate.Value.DayNumber - lr.StartDate.Value.DayNumber) + 1 : 0);

                summary.Add("TotalRequests", totalRequests);
                summary.Add("ApprovedRequests", approvedRequests);
                summary.Add("PendingRequests", pendingRequests);
                summary.Add("TotalApprovedDays", totalApprovedDays);
                summary.Add("Year", currentYear);

                if (userId.HasValue)
                {
                    var user = await _context.Users.FindAsync(userId.Value);
                    summary.Add("UserName", user?.Name ?? "Unknown");
                    summary.Add("RemainingLeave", await GetRemainingLeaveDaysAsync(userId.Value, currentYear));
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave request summary");
                return new Dictionary<string, object>();
            }
        }

        #endregion
    }
}
