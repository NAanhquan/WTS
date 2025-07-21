using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceTrackingSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IAuthenticationService _authService;

        public IndexModel(IAuthenticationService authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            return currentUser.RoleName.ToLower() switch
            {
                "admin" => RedirectToPage("/Admin/Dashboard"),
                "manager" => RedirectToPage("/Manager/Dashboard"),
                "employee" => RedirectToPage("/Employee/Dashboard"),
                _ => RedirectToPage("/Employee/Dashboard")
            };
        }
    }
}
