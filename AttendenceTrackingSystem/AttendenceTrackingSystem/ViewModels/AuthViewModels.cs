using System.ComponentModel.DataAnnotations;

namespace AttendanceTrackingSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class UserProfileViewModel
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string RoleName { get; set; } = "";
        public List<string> Permissions { get; set; } = new();
    }
}
