using Microsoft.JSInterop;
using PingMe.Frontend.Models;
using System.Net.Http.Json;

namespace PingMe.Frontend.Services;

public class WebhookService : ApiService
{
    public WebhookService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<WebhookResponse> CreateWebhookAsync(CreateWebhookRequest request)
    {
        return await PostAndGetJsonAsync<WebhookResponse>("webhooks", request) ?? new();
    }

    public async Task<List<WebhookResponse>> GetGroupWebhooksAsync(int groupId)
    {
        return await GetFromJsonAsync<List<WebhookResponse>>($"webhooks/group/{groupId}") ?? new();
    }

    public async Task<bool> DeleteWebhookAsync(int webhookId)
    {
        var response = await DeleteAsync($"webhooks/{webhookId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleWebhookAsync(int webhookId, bool active)
    {
        var response = await PatchAsync($"webhooks/{webhookId}/toggle?active={active}", new { });
        return response.IsSuccessStatusCode;
    }
}