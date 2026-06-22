using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Auth;
using PingMe.Services;

namespace PingMe.Controllers;

[ApiController]
[Route("api/auth/forgot-password")]
[AllowAnonymous]
public class ForgotPasswordController : ControllerBase
{
    private readonly ForgotPasswordService _forgotPasswordService;

    public ForgotPasswordController(ForgotPasswordService forgotPasswordService)
    {
        _forgotPasswordService = forgotPasswordService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestOtp([FromBody] ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { Message = "Email không hợp lệ" });
        }

        try
        {
            await _forgotPasswordService.RequestOtpAsync(request.Email);
            return Ok(new { Message = "Nếu email tồn tại, mã OTP đã được gửi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo mật khẩu mới ngẫu nhiên, gửi về email đã đăng ký và đặt lại mật khẩu user theo mật khẩu đó.
    /// </summary>
    [HttpPost("send-new-password")]
    public async Task<IActionResult> SendNewPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { Message = "Email không hợp lệ" });
        }

        try
        {
            await _forgotPasswordService.ResetAndEmailNewPasswordAsync(request.Email);
            return Ok(new { Message = "Nếu email tồn tại, mật khẩu mới đã được gửi về hộp thư của bạn." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
        {
            return BadRequest(new { valid = false });
        }

        var isValid = await _forgotPasswordService.VerifyOtpAsync(request.Email, request.OtpCode);
        return Ok(new { valid = isValid });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { Message = "Vui lòng nhập đầy đủ thông tin" });
        }

        var (success, error) = await _forgotPasswordService.ResetPasswordAsync(request.Email, request.OtpCode, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { Message = error });
        }

        return Ok(new { Message = "Mật khẩu đã được đặt lại thành công." });
    }
}
