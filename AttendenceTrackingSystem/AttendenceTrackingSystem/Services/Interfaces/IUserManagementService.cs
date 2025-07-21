using AttendanceTrackingSystem.ViewModels;

namespace AttendanceTrackingSystem.Services.Interfaces
{
    public interface IUserManagementService
    {
        // Admin functions
        Task<List<UserViewModel>> GetAllUsersAsync();
        Task<(bool Success, string Message)> CreateUserAsync(CreateUserViewModel model);
        Task<(bool Success, string Message)> UpdateUserAsync(UpdateUserViewModel model);
        Task<(bool Success, string Message)> DeleteUserAsync(int userId);
        Task<(bool Success, string Message)> ResetPasswordAsync(int userId);

        // Manager functions
        Task<List<UserViewModel>> GetTeamMembersAsync(int managerId);

        // Employee functions
        Task<UserProfileViewModel?> GetProfileAsync(int userId);
        Task<(bool Success, string Message)> UpdateProfileAsync(UpdateProfileViewModel model);
        Task<(bool Success, string Message)> ChangePasswordAsync(ChangePasswordViewModel model);
    }
}
