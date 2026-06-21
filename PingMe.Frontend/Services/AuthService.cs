using PingMe.Frontend.Models;
using Microsoft.JSInterop;
using System.Net.Http.Json;
namespace PingMe.Frontend.Services;

public class AuthService : ApiService
{
    public AuthService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await PostAsync("auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result != null)
                await _js.InvokeVoidAsync("localStorage.setItem", "auth_token", result.Token);
            return result;
        }
        return null;
    }

    /// <summary>Kiểm tra xem login bị lỗi EMAIL_NOT_VERIFIED không, trả về email để redirect.</summary>
    public async Task<(bool Success, string? ErrorCode, string? Email, LoginResponse? Response)> LoginDetailedAsync(LoginRequest request)
    {
        var response = await PostAsync("auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token != null)
                await _js.InvokeVoidAsync("localStorage.setItem", "auth_token", result.Token);
            return (true, null, null, result);
        }
        try
        {
            var error = await response.Content.ReadFromJsonAsync<LoginErrorResponse>();
            return (false, error?.Message, error?.Email, null);
        }
        catch { return (false, null, null, null); }
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var response = await PostAsync("auth/register", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string? Message)> RegisterWithMessageAsync(RegisterRequest request)
    {
        var response = await PostAsync("auth/register", request);
        if (response.IsSuccessStatusCode) return (true, null);
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return (false, error?.Message ?? "Đăng ký thất bại.");
        }
        catch { return (false, "Đăng ký thất bại."); }
    }

    /// <summary>Đăng ký và trả về email để FE redirect sang trang xác minh.</summary>
    public async Task<(bool Success, string? Email, string? Message)> RegisterWithEmailAsync(RegisterRequest request)
    {
        var response = await PostAsync("auth/register", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<RegisterPendingResponse>();
            return (true, result?.Email ?? request.Email, null);
        }
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return (false, null, error?.Message ?? "Đăng ký thất bại.");
        }
        catch { return (false, null, "Đăng ký thất bại."); }
    }

    public async Task<(bool Success, LoginResponse? Response, string? Message)> VerifyEmailAsync(string email, string otpCode)
    {
        var response = await PostAsync("auth/verify-email", new { email, otpCode });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token != null)
                await _js.InvokeVoidAsync("localStorage.setItem", "auth_token", result.Token);
            return (true, result, null);
        }
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return (false, null, error?.Message ?? "Xác minh thất bại.");
        }
        catch { return (false, null, "Xác minh thất bại."); }
    }

    public async Task<(bool Success, string? Message)> ResendVerifyEmailAsync(string email)
    {
        var response = await PostAsync("auth/resend-verify-email", new { email });
        if (response.IsSuccessStatusCode) return (true, null);
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return (false, error?.Message ?? "Không thể gửi lại mã.");
        }
        catch { return (false, "Không thể gửi lại mã."); }
    }

    public async Task<(bool Success, string? Message)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var response = await PostAsync("auth/change-password", new { currentPassword, newPassword });
        if (response.IsSuccessStatusCode) return (true, null);
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return (false, error?.Message ?? "Đổi mật khẩu thất bại.");
        }
        catch { return (false, "Đổi mật khẩu thất bại."); }
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        return await GetFromJsonAsync<UserDto>("auth/me");
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var response = await PostAsync("auth/forgot-password", new { Email = email });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var response = await PostAsync("auth/reset-password", new { Token = token, NewPassword = newPassword });
        return response.IsSuccessStatusCode;
    }

    private class ApiErrorResponse
    {
        public string? Message { get; set; }
    }

    private class LoginErrorResponse
    {
        public string? Message { get; set; }
        public string? Email { get; set; }
    }

    private class RegisterPendingResponse
    {
        public string? Email { get; set; }
        public string? Message { get; set; }
    }
}
