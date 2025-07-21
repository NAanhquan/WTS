using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AttendenceTrackingSystem.Pages.Employee.LeaveRequests
{
    public class IndexModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly ILeaveRequestService _leaveRequestService;

        public IndexModel(
            IAuthenticationService authService,
            ILeaveRequestService leaveRequestService)
        {
            _authService = authService;
            _leaveRequestService = leaveRequestService;
        }
        public List<LeaveRequestViewModel> LeaveRequests { get; set; } = new();
        public LeaveBalanceViewModel LeaveBalance { get; set; } = new();
        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; }

        [BindProperty]
        public LeaveRequestFilterViewModel Filter { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            await LoadLeaveDataAsync(currentUser.UserId);
            return Page();
        }

        public async Task<IActionResult> OnPostFilterAsync()
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            await LoadLeaveDataAsync(currentUser.UserId);
            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int leaveRequestId)
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var result = await _leaveRequestService.CancelLeaveRequestAsync(leaveRequestId, currentUser.UserId);
            Message = result.Message;
            IsSuccess = result.Success;

            await LoadLeaveDataAsync(currentUser.UserId);
            return Page();
        }

        private async Task LoadLeaveDataAsync(int userId)
        {
            // Load user's leave requests
            LeaveRequests = await _leaveRequestService.GetUserLeaveRequestsAsync(userId);

            // Apply filter if specified
            if (!string.IsNullOrEmpty(Filter.Status))
            {
                LeaveRequests = LeaveRequests.Where(lr => lr.Status.Equals(Filter.Status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (Filter.FromDate.HasValue)
            {
                LeaveRequests = LeaveRequests.Where(lr => lr.StartDate >= Filter.FromDate.Value).ToList();
            }

            if (Filter.ToDate.HasValue)
            {
                LeaveRequests = LeaveRequests.Where(lr => lr.EndDate <= Filter.ToDate.Value).ToList();
            }

            // Load leave balance
            LeaveBalance = await _leaveRequestService.GetUserLeaveBalanceAsync(userId, DateTime.Now.Year);
        }
    }
}
