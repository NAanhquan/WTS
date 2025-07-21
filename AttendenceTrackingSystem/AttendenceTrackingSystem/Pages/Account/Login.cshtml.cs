using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;

namespace AttendanceTrackingSystem.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAuthenticationService _authService;

        public LoginModel(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public LoginViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            Input.ReturnUrl = returnUrl;

            // Check if already logged in
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                return RedirectToRoleDashboard(currentUser.RoleName);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var result = await _authService.LoginAsync(Input);

            if (result.Success && result.User != null)
            {
                var returnUrl = Input.ReturnUrl ?? GetDefaultDashboard(result.User.RoleName);
                return LocalRedirect(returnUrl);
            }

            ErrorMessage = result.Message;
            return Page();
        }

        private IActionResult RedirectToRoleDashboard(string roleName)
        {
            return roleName.ToLower() switch
            {
                "admin" => RedirectToPage("/Admin/Dashboard"),
                "manager" => RedirectToPage("/Manager/Dashboard"),
                "employee" => RedirectToPage("/Employee/Dashboard"),
                _ => RedirectToPage("/Employee/Dashboard")
            };
        }

        private string GetDefaultDashboard(string roleName)
        {
            return roleName.ToLower() switch
            {
                "admin" => "/Admin/Dashboard",
                "manager" => "/Manager/Dashboard",
                "employee" => "/Employee/Dashboard",
                _ => "/Employee/Dashboard"
            };
        }
    }
}
