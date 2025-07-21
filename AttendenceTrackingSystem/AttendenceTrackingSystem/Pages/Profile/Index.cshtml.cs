using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendanceTrackingSystem.Pages.Profile
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

        public UserProfileViewModel User { get; set; } = new();

        [BindProperty]
        public UpdateProfileViewModel Input { get; set; } = new();

        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            User = currentUser;

            Input = new UpdateProfileViewModel
            {
                UserId = currentUser.UserId,
                Name = currentUser.Name,
                Email = currentUser.Email,
                Department = currentUser.Department,
                Position = currentUser.Position
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                User = await _authService.GetCurrentUserAsync() ?? new();
                return Page();
            }

            var result = await _userService.UpdateProfileAsync(Input);
            Message = result.Message;
            IsSuccess = result.Success;

            User = await _authService.GetCurrentUserAsync() ?? new();

            return Page();
        }
    }
}
