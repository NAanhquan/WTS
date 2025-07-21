using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceTrackingSystem.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly IAuthenticationService _authService;

        public LogoutModel(IAuthenticationService authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await _authService.LogoutAsync();
            return RedirectToPage("/Account/Login");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _authService.LogoutAsync();
            return RedirectToPage("/Account/Login");
        }
    }
}
