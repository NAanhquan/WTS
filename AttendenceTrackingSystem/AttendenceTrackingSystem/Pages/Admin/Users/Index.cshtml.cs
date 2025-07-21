using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceTrackingSystem.Pages.Admin.Users
{
    public class IndexModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserManagementService _userService;

        public IndexModel(
            IAuthenticationService authService,
            IUserManagementService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

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

            Users = await _userService.GetAllUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(int userId)
        {
            var result = await _userService.ResetPasswordAsync(userId);
            Message = result.Message;
            IsSuccess = result.Success;

            Users = await _userService.GetAllUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            Message = result.Message;
            IsSuccess = result.Success;

            Users = await _userService.GetAllUsersAsync();
            return Page();
        }
    }
}
