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

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var response = await PostAsync("auth/register", request);
        return response.IsSuccessStatusCode;
    }
    public async Task<(bool Success, string? Message)> RegisterWithMessageAsync(RegisterRequest request)
    {
        var response = await PostAsync("auth/register", request);

        if (response.IsSuccessStatusCode)
            return (true, null);

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return (false, error?.Message ?? "Đăng ký thất bại.");
        }
        catch
        {
            return (false, "Đăng ký thất bại.");
        }
    }

    private class ApiErrorResponse
    {
        public string? Message { get; set; }
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
}