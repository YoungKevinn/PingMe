using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace PingMe.Frontend.Services;

public class ForgotPasswordService : ApiService
{
    public ForgotPasswordService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<bool> RequestOtpAsync(string email)
    {
        var response = await PostAsync("auth/forgot-password/request", new { email });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> VerifyOtpAsync(string email, string otpCode)
    {
        var response = await PostAsync("auth/forgot-password/verify", new { email, otpCode });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<VerifyResponse>();
            return result?.Valid ?? false;
        }
        return false;
    }

    public async Task<(bool success, string? error)> ResetPasswordAsync(string email, string otpCode, string newPassword)
    {
        var response = await PostAsync("auth/forgot-password/reset", new { email, otpCode, newPassword });
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, errorResult?.Message ?? "Có lỗi xảy ra");
    }

    private class VerifyResponse
    {
        public bool Valid { get; set; }
    }

    private class ErrorResponse
    {
        public string Message { get; set; } = "";
    }
}
