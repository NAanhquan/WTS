using System.ComponentModel.DataAnnotations;

namespace AttendanceTrackingSystem.ViewModels
{
    public class UserViewModel
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string RoleName { get; set; } = "";
        public int RoleId { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Display properties
        public string LastLoginDisplay => LastLoginDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa đăng nhập";
        public string StatusDisplay => IsActive ? "Hoạt động" : "Vô hiệu hóa";
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Phòng ban là bắt buộc")]
        [StringLength(100, ErrorMessage = "Phòng ban không được vượt quá 100 ký tự")]
        public string Department { get; set; } = "";

        [Required(ErrorMessage = "Chức vụ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Chức vụ không được vượt quá 100 ký tự")]
        public string Position { get; set; } = "";

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateUserViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Phòng ban là bắt buộc")]
        [StringLength(100, ErrorMessage = "Phòng ban không được vượt quá 100 ký tự")]
        public string Department { get; set; } = "";

        [Required(ErrorMessage = "Chức vụ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Chức vụ không được vượt quá 100 ký tự")]
        public string Position { get; set; } = "";

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateProfileViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = "";

        [StringLength(100, ErrorMessage = "Phòng ban không được vượt quá 100 ký tự")]
        public string Department { get; set; } = "";

        [StringLength(100, ErrorMessage = "Chức vụ không được vượt quá 100 ký tự")]
        public string Position { get; set; } = "";
    }

    public class ChangePasswordViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmNewPassword { get; set; } = "";
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
