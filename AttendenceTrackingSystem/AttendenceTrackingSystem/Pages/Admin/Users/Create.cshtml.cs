using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Models;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendanceTrackingSystem.Pages.Admin.Users
{
    public class CreateModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserManagementService _userService;
        private readonly AttendanceTrackingSystemContext _context;

        public CreateModel(
            IAuthenticationService authService,
            IUserManagementService userService,
            AttendanceTrackingSystemContext context)
        {
            _authService = authService;
            _userService = userService;
            _context = context;
        }

        [BindProperty]
        public CreateUserViewModel Input { get; set; } = new();

        public SelectList RoleSelectList { get; set; } = new SelectList(new List<Role>(), "RoleId", "RoleName");

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

            await LoadRolesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadRolesAsync();
                return Page();
            }

            var result = await _userService.CreateUserAsync(Input);

            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["IsSuccess"] = true;
                return RedirectToPage("Index");
            }

            ModelState.AddModelError("", result.Message);
            await LoadRolesAsync();
            return Page();
        }

        private async Task LoadRolesAsync()
        {
            var roles = await _context.Roles.ToListAsync();
            RoleSelectList = new SelectList(roles, "RoleId", "RoleName");
        }
    }
}
