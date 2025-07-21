using AttendanceTrackingSystem.ViewModels;

namespace AttendanceTrackingSystem.Services.Interfaces
{
    public interface IAttendanceService
    {
        // Employee functions
        Task<(bool Success, string Message)> CheckInAsync(AttendanceCheckInViewModel model);
        Task<(bool Success, string Message)> CheckOutAsync(AttendanceCheckOutViewModel model);
        Task<TodayAttendanceStatusViewModel> GetTodayAttendanceStatusAsync(int userId);
        Task<AttendanceRecordViewModel?> GetTodayAttendanceAsync(int userId);
        Task<List<AttendanceRecordViewModel>> GetUserAttendanceHistoryAsync(int userId, DateTime fromDate, DateTime toDate);
        Task<AttendanceReportViewModel> GenerateUserAttendanceReportAsync(int userId, DateTime fromDate, DateTime toDate);

        // Manager/Admin functions
        Task<List<AttendanceRecordViewModel>> GetAllAttendanceAsync(DateTime? date = null);
        Task<List<AttendanceRecordViewModel>> GetFilteredAttendanceAsync(AttendanceFilterViewModel filter);
        Task<List<AttendanceRecordViewModel>> GetDepartmentAttendanceAsync(string department, DateTime? date = null);
        Task<AttendanceReportViewModel> GenerateAttendanceReportAsync(AttendanceFilterViewModel filter);

        // Utility functions
        Task<bool> HasActiveAttendanceAsync(int userId);
        Task<bool> CanCheckInAsync(int userId);
        Task<bool> CanCheckOutAsync(int userId);
        Task<int> GetActiveUsersCountAsync(DateTime? date = null);
        Task<List<AttendanceRecordViewModel>> GetLateCheckInsAsync(DateTime date);
        Task<List<AttendanceRecordViewModel>> GetEarlyCheckOutsAsync(DateTime date);

        // Admin functions
        Task<(bool Success, string Message)> UpdateAttendanceAsync(int attendanceId, DateTime? checkIn, DateTime? checkOut);
        Task<(bool Success, string Message)> DeleteAttendanceAsync(int attendanceId);
        Task<(bool Success, string Message)> AddManualAttendanceAsync(int userId, DateTime checkIn, DateTime? checkOut, string reason);
    }
}
