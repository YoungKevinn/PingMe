using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Auth;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Đăng ký tài khoản mới — trả về email để FE redirect sang trang xác minh OTP</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, _) = await _auth.RegisterAsync(request, ip, userAgent);

        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Mã OTP đã được gửi về email của bạn.", email = request.Email.Trim().ToLowerInvariant() });
    }

    /// <summary>Xác minh OTP email sau đăng ký — trả về JWT nếu hợp lệ</summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _auth.VerifyEmailOtpAsync(request.Email, request.OtpCode, ip, userAgent);
        if (!success) return BadRequest(new { message = error });
        return Ok(response);
    }

    /// <summary>Gửi lại OTP xác minh email</summary>
    [HttpPost("resend-verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerifyEmail([FromBody] ResendVerifyEmailRequest request)
    {
        var (success, error) = await _auth.ResendEmailOtpAsync(request.Email);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Nếu email đang chờ xác minh, mã mới đã được gửi." });
    }

    /// <summary>Đăng nhập — trả về JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _auth.LoginAsync(request, ip, userAgent);

        if (!success)
        {
            // Trả code riêng để FE redirect sang trang xác minh email
            if (error == "EMAIL_NOT_VERIFIED")
                return StatusCode(403, new { message = "EMAIL_NOT_VERIFIED", email = request.Email });
            return Unauthorized(new { message = error });
        }
        return Ok(response);
    }

    /// <summary>Đổi mật khẩu (cần JWT)</summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")!);

        var (success, error) = await _auth.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    /// <summary>Gửi email đặt lại mật khẩu</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _auth.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu." });
    }

    /// <summary>Đặt lại mật khẩu bằng token từ email</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (success, error) = await _auth.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }

    /// <summary>Lấy thông tin user hiện tại (cần JWT)</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")!);

        var user = await _auth.GetCurrentUserAsync(userId);
        if (user is null) return NotFound();
        return Ok(user);
    }

    private string GetIpAddress()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
