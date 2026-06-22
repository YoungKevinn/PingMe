using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class OneTimeSecretService : ApiService
{
    public OneTimeSecretService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<(CreateOneTimeSecretResponse? Secret, string? Error)> CreateAsync(CreateOneTimeSecretRequest request)
    {
        try
        {
            var response = await PostAsync("one-time-secrets", request);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<CreateOneTimeSecretResponse>(), null);

            return (null, await ReadErrorAsync(response));
        }
        catch
        {
            return (null, "Không thể tạo secret.");
        }
    }

    public async Task<List<OneTimeSecretListItemDto>> GetMineAsync()
    {
        return await GetFromJsonAsync<List<OneTimeSecretListItemDto>>("one-time-secrets")
               ?? new List<OneTimeSecretListItemDto>();
    }

    public async Task<(bool Success, string? Error)> RevokeAsync(int id)
    {
        try
        {
            var response = await PostAsync($"one-time-secrets/{id}/revoke", new { });
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await ReadErrorAsync(response));
        }
        catch
        {
            return (false, "Không thể thu hồi secret.");
        }
    }

    public async Task<(ViewOneTimeSecretResponse? Secret, string? Error)> OpenAsync(string token)
    {
        try
        {
            var response = await GetAsync($"one-time-secrets/open/{Uri.EscapeDataString(token)}");

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ViewOneTimeSecretResponse>(), null);

            return (null, await ReadErrorAsync(response));
        }
        catch
        {
            return (null, "Secret đã hết hạn hoặc đã được xem.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return "Yêu cầu không thành công.";

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? "Yêu cầu không thành công.";
        }
        catch
        {
            // Ignore parse errors and return a neutral error.
        }

        return "Yêu cầu không thành công.";
    }
}
