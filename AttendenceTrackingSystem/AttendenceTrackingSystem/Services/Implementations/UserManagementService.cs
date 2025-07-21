using AttendanceTrackingSystem.Services.Interfaces;
using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AttendanceTrackingSystem.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly AttendanceTrackingSystemContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            AttendanceTrackingSystemContext context,
            ILogger<UserManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Admin Functions

        public async Task<List<UserViewModel>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .OrderBy(u => u.Name)
                    .Select(u => new UserViewModel
                    {
                        UserId = u.UserId,
                        Name = u.Name ?? "",
                        Username = u.Username ?? "",
                        Email = u.Email ?? "",
                        Department = u.Department ?? "",
                        Position = u.Position ?? "",
                        RoleName = u.Role!.RoleName ?? "",
                        RoleId = u.RoleId,
                        IsActive = true // Assuming all users are active, adjust if you have this field
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new List<UserViewModel>();
            }
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(CreateUserViewModel model)
        {
            try
            {
                // Check if username or email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser != null)
                {
                    return (false, "Tên đăng nhập hoặc email đã tồn tại.");
                }

                // Validate role exists
                var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == model.RoleId);
                if (!roleExists)
                {
                    return (false, "Vai trò không hợp lệ.");
                }

                var user = new User
                {
                    Name = model.Name,
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    Department = model.Department,
                    Position = model.Position,
                    RoleId = model.RoleId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} created successfully", model.Username);
                return (true, "Tạo người dùng thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", model.Username);
                return (false, "Có lỗi xảy ra khi tạo người dùng.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(UpdateUserViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng.");
                }

                // Check if email is being changed and already exists
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.UserId != model.UserId);

                    if (emailExists)
                    {
                        return (false, "Email đã được sử dụng bởi người dùng khác.");
                    }
                }

                // Validate role exists
                var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == model.RoleId);
                if (!roleExists)
                {
                    return (false, "Vai trò không hợp lệ.");
                }

                user.Name = model.Name;
                user.Email = model.Email;
                user.Department = model.Department;
                user.Position = model.Position;
                user.RoleId = model.RoleId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated successfully", model.UserId);
                return (true, "Cập nhật người dùng thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", model.UserId);
                return (false, "Có lỗi xảy ra khi cập nhật người dùng.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng.");
                }

                // Check if user has related data
                var hasAttendance = await _context.Attendances.AnyAsync(a => a.UserId == userId);
                var hasLeaveRequests = await _context.LeaveRequests.AnyAsync(lr => lr.UserId == userId);
                var hasComplaints = await _context.Complaints.AnyAsync(c => c.UserId == userId);

                if (hasAttendance || hasLeaveRequests || hasComplaints)
                {
                    return (false, "Không thể xóa người dùng vì đã có dữ liệu liên quan trong hệ thống.");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted successfully", userId);
                return (true, "Xóa người dùng thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return (false, "Có lỗi xảy ra khi xóa người dùng.");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng.");
                }

                // Generate new password (you might want to make this more secure)
                var newPassword = GenerateRandomPassword();
                user.PasswordHash = HashPassword(newPassword);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for user {UserId}", userId);
                return (true, $"Đặt lại mật khẩu thành công. Mật khẩu mới: {newPassword}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                return (false, "Có lỗi xảy ra khi đặt lại mật khẩu.");
            }
        }

        #endregion

        #region Manager Functions

        public async Task<List<UserViewModel>> GetTeamMembersAsync(int managerId)
        {
            try
            {
                // Get manager's department
                var manager = await _context.Users.FindAsync(managerId);
                if (manager == null) return new List<UserViewModel>();

                return await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Department == manager.Department && u.UserId != managerId)
                    .OrderBy(u => u.Name)
                    .Select(u => new UserViewModel
                    {
                        UserId = u.UserId,
                        Name = u.Name ?? "",
                        Username = u.Username ?? "",
                        Email = u.Email ?? "",
                        Department = u.Department ?? "",
                        Position = u.Position ?? "",
                        RoleName = u.Role!.RoleName ?? "",
                        RoleId = u.RoleId,
                        IsActive = true
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members for manager {ManagerId}", managerId);
                return new List<UserViewModel>();
            }
        }

        #endregion

        #region Employee Functions

        public async Task<UserProfileViewModel?> GetProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return null;

                return new UserProfileViewModel
                {
                    UserId = user.UserId,
                    Name = user.Name ?? "",
                    Username = user.Username ?? "",
                    Email = user.Email ?? "",
                    Department = user.Department ?? "",
                    Position = user.Position ?? "",
                    RoleName = user.Role?.RoleName ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateProfileAsync(UpdateProfileViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng.");
                }

                // Check if email is being changed and already exists
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.UserId != model.UserId);

                    if (emailExists)
                    {
                        return (false, "Email đã được sử dụng bởi người dùng khác.");
                    }
                }

                user.Name = model.Name;
                user.Email = model.Email;
                // Note: Department and Position might be read-only for employees depending on business rules

                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated for user {UserId}", model.UserId);
                return (true, "Cập nhật hồ sơ thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", model.UserId);
                return (false, "Có lỗi xảy ra khi cập nhật hồ sơ.");
            }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(ChangePasswordViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng.");
                }

                // Verify current password
                var currentPasswordHash = HashPassword(model.CurrentPassword);
                if (user.PasswordHash != currentPasswordHash)
                {
                    return (false, "Mật khẩu hiện tại không chính xác.");
                }

                // Update password
                user.PasswordHash = HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed for user {UserId}", model.UserId);
                return (true, "Đổi mật khẩu thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", model.UserId);
                return (false, "Có lỗi xảy ra khi đổi mật khẩu.");
            }
        }

        #endregion

        #region Helper Methods

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}
