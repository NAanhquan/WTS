using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceTrackingSystem.Services.Implementations
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AttendanceTrackingSystemContext _context;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(
            AttendanceTrackingSystemContext context,
            ILogger<AttendanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Employee Functions

        public async Task<(bool Success, string Message)> CheckInAsync(AttendanceCheckInViewModel model)
        {
            try
            {
                // Check if user already checked in today
                if (await HasActiveAttendanceAsync(model.UserId))
                {
                    return (false, "Bạn đã chấm công vào hôm nay rồi.");
                }

                // Check if user can check in (not too early, not too late)
                if (!await CanCheckInAsync(model.UserId))
                {
                    var now = DateTime.Now.TimeOfDay;
                    if (now < new TimeSpan(5, 0, 0)) // Before 5:00 AM
                    {
                        return (false, "Chấm công quá sớm. Vui lòng chấm công sau 5:00 sáng.");
                    }
                    if (now > new TimeSpan(12, 0, 0)) // After 12:00 PM
                    {
                        return (false, "Chấm công quá muộn. Vui lòng liên hệ quản lý để được hỗ trợ.");
                    }
                }

                var attendance = new Attendance
                {
                    UserId = model.UserId,
                    CheckIn = model.CheckInTime
                    // Note: Location and Notes fields don't exist in your current model
                    // You might want to add them to the database
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                var message = model.IsLateCheckIn ?
                    "Chấm công thành công. Lưu ý: Bạn đã chấm công muộn." :
                    "Chấm công thành công. Chúc bạn một ngày làm việc hiệu quả!";

                _logger.LogInformation("User {UserId} checked in at {CheckInTime}",
                    model.UserId, model.CheckInTime);

                return (true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-in for user {UserId}", model.UserId);
                return (false, "Có lỗi xảy ra khi chấm công. Vui lòng thử lại.");
            }
        }

        public async Task<(bool Success, string Message)> CheckOutAsync(AttendanceCheckOutViewModel model)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AttendanceId == model.AttendanceId && a.CheckOut == null);

                if (attendance == null)
                {
                    return (false, "Không tìm thấy bản ghi chấm công hoặc bạn đã chấm công ra rồi.");
                }

                // Check if check-out time is valid (not before check-in)
                if (model.CheckOutTime <= attendance.CheckIn)
                {
                    return (false, "Thời gian chấm công ra không hợp lệ.");
                }

                attendance.CheckOut = model.CheckOutTime;
                await _context.SaveChangesAsync();

                var workingHours = model.CheckOutTime - attendance.CheckIn;
                var message = model.IsEarlyCheckOut ?
                    $"Chấm công ra thành công. Lưu ý: Bạn đã ra sớm. Thời gian làm việc: {workingHours.Value.Hours}h {workingHours.Value.Minutes}m" :
                    $"Chấm công ra thành công. Thời gian làm việc: {workingHours.Value.Hours}h {workingHours.Value.Minutes}m. Cảm ơn bạn đã làm việc chăm chỉ!";

                _logger.LogInformation("User {UserId} checked out at {CheckOutTime}, worked {WorkingHours}",
                    attendance.UserId, model.CheckOutTime, workingHours);

                return (true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-out for attendance {AttendanceId}", model.AttendanceId);
                return (false, "Có lỗi xảy ra khi chấm công ra. Vui lòng thử lại.");
            }
        }

        public async Task<TodayAttendanceStatusViewModel> GetTodayAttendanceStatusAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.CheckIn.Value.Date == today);

                if (attendance == null)
                {
                    return new TodayAttendanceStatusViewModel
                    {
                        HasCheckedIn = false,
                        HasCheckedOut = false
                    };
                }

                return new TodayAttendanceStatusViewModel
                {
                    HasCheckedIn = true,
                    HasCheckedOut = attendance.CheckOut.HasValue,
                    CheckInTime = attendance.CheckIn,
                    CheckOutTime = attendance.CheckOut,
                    AttendanceId = attendance.AttendanceId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today attendance status for user {UserId}", userId);
                return new TodayAttendanceStatusViewModel();
            }
        }

        public async Task<AttendanceRecordViewModel?> GetTodayAttendanceAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;
                var attendance = await _context.Attendances
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.CheckIn.Value.Date == today);

                if (attendance == null) return null;

                return new AttendanceRecordViewModel
                {
                    AttendanceId = attendance.AttendanceId,
                    UserId = attendance.UserId,
                    EmployeeName = attendance.User?.Name ?? "",
                    Department = attendance.User?.Department ?? "",
                    CheckIn = (DateTime)attendance.CheckIn,
                    CheckOut = attendance.CheckOut
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today attendance for user {UserId}", userId);
                return null;
            }
        }

        public async Task<List<AttendanceRecordViewModel>> GetUserAttendanceHistoryAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                return await _context.Attendances
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId &&
                               a.CheckIn.Value.Date >= fromDate.Date &&
                               a.CheckIn.Value.Date <= toDate.Date)
                    .OrderByDescending(a => a.CheckIn)
                    .Select(a => new AttendanceRecordViewModel
                    {
                        AttendanceId = a.AttendanceId,
                        UserId = a.UserId,
                        EmployeeName = a.User!.Name ?? "",
                        Department = a.User.Department ?? "",
                        CheckIn = (DateTime)a.CheckIn,
                        CheckOut = a.CheckOut
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance history for user {UserId}", userId);
                return new List<AttendanceRecordViewModel>();
            }
        }

        public async Task<AttendanceReportViewModel> GenerateUserAttendanceReportAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                var records = await GetUserAttendanceHistoryAsync(userId, fromDate, toDate);

                return new AttendanceReportViewModel
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    UserId = userId,
                    EmployeeName = user?.Name ?? "",
                    Department = user?.Department ?? "",
                    Records = records
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report for user {UserId}", userId);
                return new AttendanceReportViewModel
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    UserId = userId
                };
            }
        }

        #endregion

        #region Utility Functions

        public async Task<bool> HasActiveAttendanceAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;
                return await _context.Attendances
                    .AnyAsync(a => a.UserId == userId && a.CheckIn.Value.Date == today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active attendance for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CanCheckInAsync(int userId)
        {
            try
            {
                var now = DateTime.Now.TimeOfDay;

                // Allow check-in between 5:00 AM and 12:00 PM
                if (now < new TimeSpan(5, 0, 0) || now > new TimeSpan(12, 0, 0))
                    return false;

                // Check if already checked in today
                return !await HasActiveAttendanceAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking can check-in for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CanCheckOutAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;
                return await _context.Attendances
                    .AnyAsync(a => a.UserId == userId && a.CheckIn.Value.Date == today && a.CheckOut == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking can check-out for user {UserId}", userId);
                return false;
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

        public async Task<List<AttendanceRecordViewModel>> GetLateCheckInsAsync(DateTime date)
        {
            try
            {
                var lateTime = new TimeSpan(9, 0, 0); // 9:00 AM

                return await _context.Attendances
                    .Include(a => a.User)
                    .Where(a => a.CheckIn.Value.Date == date.Date && a.CheckIn.Value.TimeOfDay > lateTime)
                    .OrderByDescending(a => a.CheckIn)
                    .Select(a => new AttendanceRecordViewModel
                    {
                        AttendanceId = a.AttendanceId,
                        UserId = a.UserId,
                        EmployeeName = a.User!.Name ?? "",
                        Department = a.User.Department ?? "",
                        CheckIn = (DateTime)a.CheckIn,
                        CheckOut = a.CheckOut
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting late check-ins for date {Date}", date);
                return new List<AttendanceRecordViewModel>();
            }
        }

        public async Task<List<AttendanceRecordViewModel>> GetEarlyCheckOutsAsync(DateTime date)
        {
            try
            {
                var earlyTime = new TimeSpan(17, 30, 0); // 5:30 PM

                return await _context.Attendances
                    .Include(a => a.User)
                    .Where(a => a.CheckIn.Value.Date == date.Date &&
                               a.CheckOut.HasValue &&
                               a.CheckOut.Value.TimeOfDay < earlyTime)
                    .OrderBy(a => a.CheckOut)
                    .Select(a => new AttendanceRecordViewModel
                    {
                        AttendanceId = a.AttendanceId,
                        UserId = a.UserId,
                        EmployeeName = a.User!.Name ?? "",
                        Department = a.User.Department ?? "",
                        CheckIn = (DateTime)a.CheckIn,
                        CheckOut = a.CheckOut
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting early check-outs for date {Date}", date);
                return new List<AttendanceRecordViewModel>();
            }
        }

        #endregion

        #region Manager/Admin Functions

        public async Task<List<AttendanceRecordViewModel>> GetAllAttendanceAsync(DateTime? date = null)
        {
            try
            {
                var query = _context.Attendances.Include(a => a.User).AsQueryable();

                if (date.HasValue)
                {
                    query = query.Where(a => a.CheckIn.Value.Date == date.Value.Date);
                }

                return await query
                    .OrderByDescending(a => a.CheckIn)
                    .Select(a => new AttendanceRecordViewModel
                    {
                        AttendanceId = a.AttendanceId,
                        UserId = a.UserId,
                        EmployeeName = a.User!.Name ?? "",
                        Department = a.User.Department ?? "",
                        CheckIn = (DateTime)a.CheckIn,
                        CheckOut = a.CheckOut
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all attendance for date {Date}", date);
                return new List<AttendanceRecordViewModel>();
            }
        }

        public async Task<List<AttendanceRecordViewModel>> GetFilteredAttendanceAsync(AttendanceFilterViewModel filter)
        {
            try
            {
                var query = _context.Attendances
                    .Include(a => a.User)
                    .AsQueryable();

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(a => a.CheckIn.Value.Date >= filter.FromDate.Value.Date);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(a => a.CheckIn.Value.Date <= filter.ToDate.Value.Date);
                }

                if (filter.UserId.HasValue)
                {
                    query = query.Where(a => a.UserId == filter.UserId.Value);
                }

                if (!string.IsNullOrEmpty(filter.Department))
                {
                    query = query.Where(a => a.User!.Department == filter.Department);
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    switch (filter.Status.ToLower())
                    {
                        case "completed":
                            query = query.Where(a => a.CheckOut.HasValue);
                            break;
                        case "active":
                            query = query.Where(a => !a.CheckOut.HasValue);
                            break;
                    }
                }

                return await query
                    .OrderByDescending(a => a.CheckIn)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(a => new AttendanceRecordViewModel
                    {
                        AttendanceId = a.AttendanceId,
                        UserId = a.UserId,
                        EmployeeName = a.User!.Name ?? "",
                        Department = a.User.Department ?? "",
                        CheckIn = (DateTime)a.CheckIn,
                        CheckOut = a.CheckOut
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered attendance");
                return new List<AttendanceRecordViewModel>();
            }
        }

        public async Task<List<AttendanceRecordViewModel>> GetDepartmentAttendanceAsync(string department, DateTime? date = null)
        {
            try
            {
                var query = _context.Attendances
                    .Include(a => a.User)
                    .Where(a => a.User!.Department == department);

                if (date.HasValue)
                {
                    query = query.Where(a => a.CheckIn.Value.Date == date.Value.Date);
                }

                return await query
                    .OrderByDescending(a => a.CheckIn)
                    .Select(a => new AttendanceRecordViewModel
                    {
                        AttendanceId = a.AttendanceId,
                        UserId = a.UserId,
                        EmployeeName = a.User!.Name ?? "",
                        Department = a.User.Department ?? "",
                        CheckIn = (DateTime)a.CheckIn,
                        CheckOut = a.CheckOut
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department attendance for {Department}", department);
                return new List<AttendanceRecordViewModel>();
            }
        }

        public async Task<AttendanceReportViewModel> GenerateAttendanceReportAsync(AttendanceFilterViewModel filter)
        {
            try
            {
                var records = await GetFilteredAttendanceAsync(filter);

                // If filtering by specific user, get user info
                string employeeName = "";
                string department = "";

                if (filter.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(filter.UserId.Value);
                    employeeName = user?.Name ?? "";
                    department = user?.Department ?? "";
                }

                return new AttendanceReportViewModel
                {
                    FromDate = filter.FromDate ?? DateTime.Today.AddMonths(-1),
                    ToDate = filter.ToDate ?? DateTime.Today,
                    UserId = filter.UserId ?? 0,
                    EmployeeName = employeeName,
                    Department = department,
                    Records = records
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report");
                return new AttendanceReportViewModel();
            }
        }

        public async Task<(bool Success, string Message)> UpdateAttendanceAsync(int attendanceId, DateTime? checkIn, DateTime? checkOut)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null)
                {
                    return (false, "Không tìm thấy bản ghi chấm công.");
                }

                // Validate input
                if (checkIn.HasValue && checkOut.HasValue && checkOut.Value <= checkIn.Value)
                {
                    return (false, "Thời gian chấm công ra phải sau thời gian chấm công vào.");
                }

                // Check if checkIn is not too far in the past or future
                if (checkIn.HasValue)
                {
                    var daysDiff = Math.Abs((checkIn.Value.Date - DateTime.Today).Days);
                    if (daysDiff > 30)
                    {
                        return (false, "Không thể chỉnh sửa chấm công quá 30 ngày.");
                    }

                    // Validate check-in time
                    var checkInTime = checkIn.Value.TimeOfDay;
                    if (checkInTime < new TimeSpan(4, 0, 0) || checkInTime > new TimeSpan(23, 59, 59))
                    {
                        return (false, "Thời gian chấm công vào không hợp lệ (4:00 - 23:59).");
                    }
                }

                // Check if checkOut is valid
                if (checkOut.HasValue)
                {
                    var checkOutTime = checkOut.Value.TimeOfDay;
                    if (checkOutTime < new TimeSpan(6, 0, 0) || checkOutTime > new TimeSpan(23, 59, 59))
                    {
                        return (false, "Thời gian chấm công ra không hợp lệ (6:00 - 23:59).");
                    }

                    // Check if working time is reasonable (not more than 16 hours)
                    if (checkIn.HasValue)
                    {
                        var workingHours = checkOut.Value - checkIn.Value;
                        if (workingHours.TotalHours > 16)
                        {
                            return (false, "Thời gian làm việc không được vượt quá 16 giờ.");
                        }
                        if (workingHours.TotalMinutes < 30)
                        {
                            return (false, "Thời gian làm việc tối thiểu là 30 phút.");
                        }
                    }
                }

                // Update attendance record
                var originalCheckIn = attendance.CheckIn;
                var originalCheckOut = attendance.CheckOut;

                if (checkIn.HasValue)
                {
                    attendance.CheckIn = checkIn.Value;
                }

                if (checkOut.HasValue)
                {
                    attendance.CheckOut = checkOut.Value;
                }
                else if (checkOut == null) // Explicitly set to null
                {
                    attendance.CheckOut = null;
                }

                await _context.SaveChangesAsync();

                // Log the change
                _logger.LogInformation(
                    "Attendance {AttendanceId} updated for user {UserId}. CheckIn: {OriginalCheckIn} -> {NewCheckIn}, CheckOut: {OriginalCheckOut} -> {NewCheckOut}",
                    attendanceId, attendance.UserId, originalCheckIn, attendance.CheckIn, originalCheckOut, attendance.CheckOut);

                return (true, "Cập nhật bản ghi chấm công thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance {AttendanceId}", attendanceId);
                return (false, "Có lỗi xảy ra khi cập nhật bản ghi chấm công.");
            }
        }


        public async Task<(bool Success, string Message)> DeleteAttendanceAsync(int attendanceId)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null)
                {
                    return (false, "Không tìm thấy bản ghi chấm công.");
                }

                // Check if the attendance record is not too old (prevent deleting old records)
                var daysDiff = Math.Abs((attendance.CheckIn?.Date - DateTime.Today)?.Days ?? 0);
                if (daysDiff > 7)
                {
                    return (false, "Không thể xóa bản ghi chấm công quá 7 ngày tuổi. Vui lòng liên hệ quản trị viên.");
                }

                // Store info for logging before deletion
                var userId = attendance.UserId;
                var userName = attendance.User?.Name ?? "Unknown";
                var checkInTime = attendance.CheckIn;

                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Attendance record {AttendanceId} deleted for user {UserId} ({UserName}). CheckIn was: {CheckInTime}",
                    attendanceId, userId, userName, checkInTime);

                return (true, $"Đã xóa bản ghi chấm công của {userName} thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance {AttendanceId}", attendanceId);
                return (false, "Có lỗi xảy ra khi xóa bản ghi chấm công.");
            }
        }


        public async Task<(bool Success, string Message)> AddManualAttendanceAsync(int userId, DateTime checkIn, DateTime? checkOut, string reason)
        {
            try
            {
                // Validate user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng.");
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return (false, "Lý do thêm chấm công thủ công là bắt buộc.");
                }

                if (reason.Length > 500)
                {
                    return (false, "Lý do không được vượt quá 500 ký tự.");
                }

                // Check if attendance already exists for this date
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == userId &&
                                            a.CheckIn.HasValue &&
                                            a.CheckIn.Value.Date == checkIn.Date);

                if (existingAttendance != null)
                {
                    return (false, $"Đã có bản ghi chấm công cho ngày {checkIn:dd/MM/yyyy}. Vui lòng sử dụng chức năng cập nhật thay vì thêm mới.");
                }

                // Validate check-in time
                var checkInTime = checkIn.TimeOfDay;
                if (checkInTime < new TimeSpan(4, 0, 0) || checkInTime > new TimeSpan(23, 59, 59))
                {
                    return (false, "Thời gian chấm công vào không hợp lệ (4:00 - 23:59).");
                }

                // Validate check-out time if provided
                if (checkOut.HasValue)
                {
                    if (checkOut.Value <= checkIn)
                    {
                        return (false, "Thời gian chấm công ra phải sau thời gian chấm công vào.");
                    }

                    var checkOutTime = checkOut.Value.TimeOfDay;
                    if (checkOutTime < new TimeSpan(6, 0, 0) || checkOutTime > new TimeSpan(23, 59, 59))
                    {
                        return (false, "Thời gian chấm công ra không hợp lệ (6:00 - 23:59).");
                    }

                    // Check working hours limit
                    var workingHours = checkOut.Value - checkIn;
                    if (workingHours.TotalHours > 16)
                    {
                        return (false, "Thời gian làm việc không được vượt quá 16 giờ.");
                    }
                    if (workingHours.TotalMinutes < 30)
                    {
                        return (false, "Thời gian làm việc tối thiểu là 30 phút.");
                    }
                }

                // Check if the date is not too far in the past
                var daysDiff = (DateTime.Today - checkIn.Date).Days;
                if (daysDiff > 30)
                {
                    return (false, "Không thể thêm chấm công cho ngày quá 30 ngày trước.");
                }

                // Check if the date is not in the future (except today)
                if (checkIn.Date > DateTime.Today)
                {
                    return (false, "Không thể thêm chấm công cho ngày trong tương lai.");
                }

                // Create new attendance record
                var attendance = new Attendance
                {
                    UserId = userId,
                    CheckIn = checkIn,
                    CheckOut = checkOut
                    // Note: You might want to add a field to store the reason and who added it manually
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                var workingTime = checkOut.HasValue ?
                    $", thời gian làm việc: {(checkOut.Value - checkIn).Hours}h {(checkOut.Value - checkIn).Minutes}m" :
                    " (chưa chấm công ra)";

                _logger.LogInformation(
                    "Manual attendance added for user {UserId} ({UserName}) on {Date}. CheckIn: {CheckIn}, CheckOut: {CheckOut}. Reason: {Reason}",
                    userId, user.Name, checkIn.Date, checkIn, checkOut, reason);

                return (true, $"Đã thêm bản ghi chấm công thủ công cho {user.Name} ngày {checkIn:dd/MM/yyyy}{workingTime}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding manual attendance for user {UserId} on {Date}", userId, checkIn.Date);
                return (false, "Có lỗi xảy ra khi thêm bản ghi chấm công thủ công.");
            }
        }

        #endregion
    }
}
