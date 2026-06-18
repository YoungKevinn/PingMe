using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Auth;
using PingMe.Models;

namespace PingMe.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error, LoginResponse? Response)> RegisterAsync(RegisterRequest request, string ipAddress, string? userAgent);
    Task<(bool Success, string? Error, LoginResponse? Response)> LoginAsync(LoginRequest request, string ipAddress, string? userAgent);
    Task<(bool Success, string? Error)> ForgotPasswordAsync(string email);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword);
    Task<UserDto?> GetCurrentUserAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IJwtService jwt, IEmailService email, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
        _config = config;
    }

    public async Task<(bool Success, string? Error, LoginResponse? Response)> RegisterAsync(
        RegisterRequest request, string ipAddress, string? userAgent)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
            return (false, "Vui lòng nhập tên người dùng.", null);

        if (string.IsNullOrWhiteSpace(email))
            return (false, "Vui lòng nhập email.", null);

        if (string.IsNullOrWhiteSpace(displayName))
            return (false, "Vui lòng nhập tên hiển thị.", null);

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "Mật khẩu phải có tối thiểu 6 ký tự.", null);

        if (await _db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            return (false, "Tên người dùng đã tồn tại.", null);

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return (false, "Email đã được sử dụng.", null);

        var user = new User
        {
            Username = username,
            Email = email,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsOnline = true,
            LastLoginIp = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await IssueTokenAsync(user, ipAddress, userAgent);
    }

    public async Task<(bool Success, string? Error, LoginResponse? Response)> LoginAsync(
        LoginRequest request, string ipAddress, string? userAgent)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "Vui lòng nhập email và mật khẩu.", null);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            return (false, "Email hoặc mật khẩu không đúng.", null);

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return (false, "Tài khoản này bị lỗi mật khẩu. Vui lòng xóa tài khoản lỗi và đăng ký lại.", null);

        bool passwordOk;

        try
        {
            passwordOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch
        {
            return (false, "Tài khoản này có mật khẩu không hợp lệ. Vui lòng đăng ký lại.", null);
        }

        if (!passwordOk)
            return (false, "Email hoặc mật khẩu không đúng.", null);

        if (user.LastLoginIp is not null && user.LastLoginIp != ipAddress)
        {
            _ = Task.Run(() => _email.SendLoginAnomalyAlertAsync(
                user.Email, user.DisplayName, ipAddress, user.LastLoginIp));
        }

        user.IsOnline = true;
        user.LastSeen = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await IssueTokenAsync(user, ipAddress, userAgent);
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null)
            return (true, null);

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _ = Task.Run(() => _email.SendPasswordResetEmailAsync(user.Email, user.DisplayName, token));

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Mật khẩu mới phải có tối thiểu 6 ký tự.");

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user is null)
            return (false, "Token không hợp lệ hoặc đã hết hạn.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        var sessions = await _db.UserSessions.Where(s => s.UserId == user.Id).ToListAsync();
        sessions.ForEach(s => s.IsRevoked = true);

        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    private async Task<(bool Success, string? Error, LoginResponse? Response)> IssueTokenAsync(
        User user, string ipAddress, string? userAgent)
    {
        var days = int.Parse(_config["Jwt:ExpirationDays"] ?? "7");
        var rawToken = _jwt.GenerateToken(user);
        var tokenHash = _jwt.HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.AddDays(days);

        _db.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            DeviceInfo = ParseDeviceInfo(userAgent),
            IpAddress = ipAddress,
            LastActive = DateTime.UtcNow,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        });

        await _db.SaveChangesAsync();

        return (true, null, new LoginResponse
        {
            Token = rawToken,
            ExpiresAt = expiresAt,
            User = MapToDto(user)
        });
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        DisplayName = user.DisplayName,
        AvatarUrl = user.AvatarUrl,
        Bio = user.Bio,
        JobTitle = user.JobTitle,
        Department = user.Department,
        WorkLocation = user.WorkLocation,
        PhoneNumber = user.PhoneNumber,
        DateOfBirth = user.DateOfBirth,
        IsOnline = user.IsOnline,
        LastSeen = user.LastSeen,
        CreatedAt = user.CreatedAt
    };

    private static string ParseDeviceInfo(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        return userAgent.Length > 200 ? userAgent[..200] : userAgent;
    }
}