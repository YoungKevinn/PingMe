using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.Models;
using System;
using System.Threading.Tasks;

namespace PingMe.Services;

public class ForgotPasswordService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;

    public ForgotPasswordService(AppDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<bool> RequestOtpAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
        {
            return true; // Luôn trả OK
        }

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var recentOtpsCount = await _db.PasswordResetOtps
            .CountAsync(o => o.UserId == user.Id && o.CreatedAt >= oneHourAgo);

        if (recentOtpsCount >= 3)
        {
            throw new Exception("Vui lòng thử lại sau 1 giờ");
        }

        var otpCode = Random.Shared.Next(100000, 999999).ToString();
        var otp = new PasswordResetOtp
        {
            UserId = user.Id,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.PasswordResetOtps.Add(otp);
        await _db.SaveChangesAsync();

        _ = Task.Run(() => _email.SendOtpEmailAsync(user.Email, user.DisplayName, otpCode));

        return true;
    }

    public async Task<bool> VerifyOtpAsync(string email, string otpCode)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null) return false;

        var otp = await _db.PasswordResetOtps
            .FirstOrDefaultAsync(o => o.UserId == user.Id &&
                                      o.OtpCode == otpCode &&
                                      !o.IsUsed &&
                                      o.ExpiresAt > DateTime.UtcNow);

        return otp != null;
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string otpCode, string newPassword)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null) return (false, "Tài khoản không tồn tại");

        var otp = await _db.PasswordResetOtps
            .FirstOrDefaultAsync(o => o.UserId == user.Id &&
                                      o.OtpCode == otpCode &&
                                      !o.IsUsed &&
                                      o.ExpiresAt > DateTime.UtcNow);

        if (otp == null) return (false, "Mã OTP không hợp lệ hoặc đã hết hạn");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return (false, "Mật khẩu mới phải có tối thiểu 8 ký tự.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        otp.IsUsed = true;

        var sessions = await _db.UserSessions.Where(s => s.UserId == user.Id).ToListAsync();
        sessions.ForEach(s => s.IsRevoked = true);

        await _db.SaveChangesAsync();

        return (true, null);
    }
}
