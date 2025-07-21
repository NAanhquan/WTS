using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceTrackingSystem.Pages.Employee.LeaveRequests
{
    public class CreateModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly ILeaveRequestService _leaveRequestService;

        public CreateModel(
            IAuthenticationService authService,
            ILeaveRequestService leaveRequestService)
        {
            _authService = authService;
            _leaveRequestService = leaveRequestService;
        }

        [BindProperty]
        public LeaveRequestCreateViewModel Input { get; set; } = new();

        public LeaveBalanceViewModel LeaveBalance { get; set; } = new();
        public SelectList LeaveTypeSelectList { get; set; } = new SelectList(new List<object>(), "Value", "Text");
        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Input.UserId = currentUser.UserId;
            await LoadDataAsync(currentUser.UserId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Input.UserId = currentUser.UserId;

            if (!ModelState.IsValid)
            {
                await LoadDataAsync(currentUser.UserId);
                return Page();
            }

            var result = await _leaveRequestService.CreateLeaveRequestAsync(Input);

            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["IsSuccess"] = true;
                return RedirectToPage("Index");
            }

            Message = result.Message;
            IsSuccess = false;

            await LoadDataAsync(currentUser.UserId);
            return Page();
        }

        private async Task LoadDataAsync(int userId)
        {
            // Load leave balance
            LeaveBalance = await _leaveRequestService.GetUserLeaveBalanceAsync(userId, DateTime.Now.Year);

            // Load leave types
            LeaveTypeSelectList = new SelectList(LeaveTypes.GetAll(), "Value", "Display");
        }
    }
}
