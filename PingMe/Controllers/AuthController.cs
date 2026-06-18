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

    /// <summary>Đăng ký tài khoản mới</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _auth.RegisterAsync(request, ip, userAgent);

        if (!success) return BadRequest(new { message = error });
        return Ok(response);
    }

    /// <summary>Đăng nhập — trả về JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _auth.LoginAsync(request, ip, userAgent);

        if (!success) return Unauthorized(new { message = error });
        return Ok(response);
    }

    /// <summary>Gửi email đặt lại mật khẩu</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _auth.ForgotPasswordAsync(request.Email);
        // Luôn trả 200 để không tiết lộ email có tồn tại hay không
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

    /// <summary>Lấy IP thực của client (hỗ trợ reverse proxy)</summary>
    private string GetIpAddress()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
