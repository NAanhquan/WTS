using AttendanceTrackingSystem.ViewModels;

namespace AttendenceTrackingSystem.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<(bool Success, UserProfileViewModel? User, string Message)> LoginAsync(LoginViewModel model);
        Task LogoutAsync();
        Task<UserProfileViewModel?> GetCurrentUserAsync();
        Task<bool> IsInRoleAsync(int userId, string roleName);
        Task<List<string>> GetUserPermissionsAsync(int userId);
    }
}
