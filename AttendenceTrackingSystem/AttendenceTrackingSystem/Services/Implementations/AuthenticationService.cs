using AttendanceTrackingSystem.ViewModels;
using AttendenceTrackingSystem.Models;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace AttendanceTrackingSystem.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AttendanceTrackingSystemContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthenticationService> _logger;
        private const string USER_SESSION_KEY = "CurrentUser";

        public AuthenticationService(
            AttendanceTrackingSystemContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthenticationService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<(bool Success, UserProfileViewModel? User, string Message)> LoginAsync(LoginViewModel model)
        {
            try
            {
                // Tìm user theo username
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user == null)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", model.Username);
                    return (false, null, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                }

                // Verify password với BCrypt
                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for username: {Username}", model.Username);
                    return (false, null, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                }

                var userProfile = new UserProfileViewModel
                {
                    UserId = user.UserId,
                    Name = user.Name ?? "",
                    Username = user.Username ?? "",
                    Email = user.Email ?? "",
                    Department = user.Department ?? "",
                    Position = user.Position ?? "",
                    RoleName = user.Role?.RoleName ?? "",
                    Permissions = await GetUserPermissionsAsync(user.UserId)
                };

                // Store user in session
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    session.SetString(USER_SESSION_KEY, System.Text.Json.JsonSerializer.Serialize(userProfile));
                }

                _logger.LogInformation("User {Username} logged in successfully", model.Username);
                return (true, userProfile, "Đăng nhập thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", model.Username);
                return (false, null, "Có lỗi xảy ra trong quá trình đăng nhập.");
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var currentUser = await GetCurrentUserAsync();
                    if (currentUser != null)
                    {
                        _logger.LogInformation("User {Username} logged out", currentUser.Username);
                    }

                    session.Remove(USER_SESSION_KEY);
                    session.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
        }

        public async Task<UserProfileViewModel?> GetCurrentUserAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null) return null;

                var userJson = session.GetString(USER_SESSION_KEY);
                if (string.IsNullOrEmpty(userJson)) return null;

                var userProfile = System.Text.Json.JsonSerializer.Deserialize<UserProfileViewModel>(userJson);

                // Verify user still exists and is active
                if (userProfile != null)
                {
                    var userExists = await _context.Users
                        .AnyAsync(u => u.UserId == userProfile.UserId);

                    if (!userExists)
                    {
                        await LogoutAsync();
                        return null;
                    }
                }

                return userProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .AnyAsync(u => u.UserId == userId &&
                                  u.Role!.RoleName!.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role for user {UserId}", userId);
                return false;
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user?.Role == null) return new List<string>();

                return GetRolePermissions(user.Role.RoleName ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return new List<string>();
            }
        }

        private List<string> GetRolePermissions(string roleName)
        {
            return roleName.ToLower() switch
            {
                "admin" => new List<string>
                {
                    "user.create", "user.read", "user.update", "user.delete",
                    "attendance.read", "attendance.update", "attendance.delete",
                    "leave.read", "leave.approve", "leave.reject",
                    "complaint.read", "complaint.resolve",
                    "report.all", "system.config"
                },
                "manager" => new List<string>
                {
                    "user.read", "attendance.read", "attendance.update",
                    "leave.read", "leave.approve", "leave.reject",
                    "complaint.read", "complaint.resolve",
                    "report.department"
                },
                "employee" => new List<string>
                {
                    "profile.read", "profile.update",
                    "attendance.checkin", "attendance.checkout", "attendance.read.own",
                    "leave.create", "leave.read.own",
                    "complaint.create", "complaint.read.own"
                },
                _ => new List<string>()
            };
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(11));
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
