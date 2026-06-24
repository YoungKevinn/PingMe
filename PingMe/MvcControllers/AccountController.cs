using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Auth;
using PingMe.Services;

namespace PingMe.MvcControllers;

[Route("auth")]
public class AccountController : Controller
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/chat");

        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Auth/Login.cshtml");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Error = "Vui lòng điền đầy đủ thông tin.";
            return View("~/Views/Auth/Login.cshtml");
        }

        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _auth.LoginAsync(model, ip, userAgent);

        if (!success)
        {
            if (error == "EMAIL_NOT_VERIFIED")
                return Redirect($"/auth/verify-email?email={Uri.EscapeDataString(model.Email)}");

            ViewBag.Error = "Email hoặc mật khẩu không đúng.";
            return View("~/Views/Auth/Login.cshtml");
        }

        // Issue cookie auth with user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response!.User.Id.ToString()),
            new Claim(ClaimTypes.Name, response.User.Username),
            new Claim("displayName", response.User.DisplayName),
            new Claim("token", response.Token),   // keep JWT for SignalR / API calls
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        // Also set non-HttpOnly cookie so JS can read the JWT for API calls
        Response.Cookies.Append("pm_token", response.Token ?? "", new CookieOptions
        {
            HttpOnly  = false,
            Secure    = Request.IsHttps,
            SameSite  = SameSiteMode.Lax,
            Expires   = DateTimeOffset.UtcNow.AddDays(30)
        });

        var dest = (returnUrl != null && Url.IsLocalUrl(returnUrl)) ? returnUrl : "/timeline";
        return Redirect(dest);
    }

    [HttpGet("register")]
    [AllowAnonymous]
    public IActionResult Register() => View("~/Views/Auth/Register.cshtml");

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Error = "Vui lòng điền đầy đủ thông tin.";
            return View("~/Views/Auth/Register.cshtml");
        }

        model.Email       = model.Email?.Trim().ToLowerInvariant() ?? "";
        model.Username    = model.Username?.Trim() ?? "";
        model.DisplayName = model.DisplayName?.Trim() ?? "";

        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, _) = await _auth.RegisterAsync(model, ip, userAgent);

        if (!success)
        {
            ViewBag.Error = error ?? "Đăng ký thất bại.";
            return View("~/Views/Auth/Register.cshtml");
        }

        return Redirect($"/auth/verify-email?email={Uri.EscapeDataString(model.Email)}");
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public IActionResult VerifyEmail() => View("~/Views/Auth/VerifyEmail.cshtml");

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(string email, string otpCode)
    {
        var ip        = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _auth.VerifyEmailOtpAsync(email, otpCode, ip, userAgent);

        if (!success)
        {
            ViewBag.Error = error ?? "Mã OTP không hợp lệ.";
            return View("~/Views/Auth/VerifyEmail.cshtml");
        }

        // Auto-login after successful verification
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response!.User.Id.ToString()),
            new Claim(ClaimTypes.Name, response.User.Username),
            new Claim("displayName", response.User.DisplayName),
            new Claim("token", response.Token),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        Response.Cookies.Append("pm_token", response.Token ?? "", new CookieOptions
        {
            HttpOnly = false, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return Redirect("/timeline");
    }

    [HttpPost("resend-otp")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string email)
    {
        await _auth.ResendEmailOtpAsync(email);
        ViewBag.Success = "Đã gửi lại mã OTP. Vui lòng kiểm tra email.";
        return View("~/Views/Auth/VerifyEmail.cshtml");
    }

    [HttpGet("forgot-password")]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View("~/Views/Auth/ForgotPassword.cshtml");

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        await _auth.ForgotPasswordAsync(email);
        ViewBag.Sent = true;
        ViewBag.SentEmail = email;
        return View("~/Views/Auth/ForgotPassword.cshtml");
    }

    [HttpGet("reset-password")]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Redirect("/auth/forgot-password");

        ViewBag.Token = token;
        return View("~/Views/Auth/ResetPassword.cshtml");
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        ViewBag.Token = token;

        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Error = "Link không hợp lệ. Vui lòng yêu cầu lại.";
            return View("~/Views/Auth/ResetPassword.cshtml");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
            return View("~/Views/Auth/ResetPassword.cshtml");
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp.";
            return View("~/Views/Auth/ResetPassword.cshtml");
        }

        var (success, error) = await _auth.ResetPasswordAsync(token, newPassword);

        if (!success)
        {
            ViewBag.Error = error ?? "Link đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại.";
            return View("~/Views/Auth/ResetPassword.cshtml");
        }

        ViewBag.Done = true;
        return View("~/Views/Auth/ResetPassword.cshtml");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("pm_token");
        return Redirect("/auth/login");
    }

    private string GetIpAddress()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
