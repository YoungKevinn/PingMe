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

    /// <summary>
    /// Tạo mật khẩu mới ngẫu nhiên, đặt lại mật khẩu cho user và gửi mật khẩu đó về email đã đăng ký.
    /// Luôn trả true để không tiết lộ email có tồn tại hay không.
    /// </summary>
    public async Task<bool> ResetAndEmailNewPasswordAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
        {
            return true; // Luôn trả OK để tránh dò email
        }

        // Giới hạn tần suất: chỉ cho yêu cầu lại sau mỗi 2 phút
        // (dùng cột PasswordResetTokenExpiry của user làm mốc, không cần bảng phụ)
        if (user.PasswordResetTokenExpiry is { } lastRequest && lastRequest > DateTime.UtcNow.AddMinutes(-2))
        {
            throw new Exception("Bạn vừa yêu cầu đặt lại mật khẩu. Vui lòng thử lại sau ít phút.");
        }

        var newPassword = GenerateRandomPassword(12);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = DateTime.UtcNow; // mốc thời điểm yêu cầu gần nhất
        user.UpdatedAt = DateTime.UtcNow;

        // Thu hồi mọi phiên đăng nhập cũ
        var sessions = await _db.UserSessions.Where(s => s.UserId == user.Id).ToListAsync();
        sessions.ForEach(s => s.IsRevoked = true);

        await _db.SaveChangesAsync();

        // Gửi mật khẩu mới về email (await để chắc chắn đã cố gửi)
        await _email.SendNewPasswordEmailAsync(user.Email, user.DisplayName, newPassword);

        return true;
    }

    private static string GenerateRandomPassword(int length)
    {
        // Tránh ký tự dễ nhầm (0/O, 1/l/I) cho người dùng gõ tay
        const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%&*";
        const string all = upper + lower + digits + special;

        var rnd = Random.Shared;
        var chars = new char[length];

        // Đảm bảo có đủ 4 nhóm ký tự
        chars[0] = upper[rnd.Next(upper.Length)];
        chars[1] = lower[rnd.Next(lower.Length)];
        chars[2] = digits[rnd.Next(digits.Length)];
        chars[3] = special[rnd.Next(special.Length)];

        for (int i = 4; i < length; i++)
            chars[i] = all[rnd.Next(all.Length)];

        // Xáo trộn để 4 ký tự bắt buộc không cố định vị trí
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
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
