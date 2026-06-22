using System.Net.Http.Json;
using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class SnippetService : ApiService
{
    public SnippetService(HttpClient http, IJSRuntime js) : base(http, js) { }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<SnippetResponse?> CreateSnippetAsync(CreateSnippetRequest request)
        => await PostAndGetJsonAsync<SnippetResponse>("snippets", request);
    public async Task<(SnippetResponse? Snippet, string? Error)> UpdateSnippetAsync(
    int id,
    UpdateSnippetRequest request)
    {
        var response = await PutAsync($"snippets/{id}", request);

        if (response.IsSuccessStatusCode)
        {
            var snippet = await response.Content.ReadFromJsonAsync<SnippetResponse>();
            return (snippet, null);
        }

        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return (null, body?.Message ?? "Không thể cập nhật snippet.");
        }
        catch
        {
            return (null, "Không thể cập nhật snippet.");
        }
    }
    public async Task<List<SnippetResponse>?> GetMySnippetsAsync()
        => await GetFromJsonAsync<List<SnippetResponse>>("snippets");

    public async Task<bool> DeleteSnippetAsync(int id)
    {
        var response = await DeleteAsync($"snippets/{id}");
        return response.IsSuccessStatusCode;
    }

    // ── Search ───────────────────────────────────────────────────────────────

    public async Task<List<SnippetResponse>?> SearchSnippetsAsync(SnippetSearchRequest request)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Title))    qs.Add($"title={Uri.EscapeDataString(request.Title)}");
        if (!string.IsNullOrWhiteSpace(request.Language)) qs.Add($"language={Uri.EscapeDataString(request.Language)}");
        if (!string.IsNullOrWhiteSpace(request.Content))  qs.Add($"content={Uri.EscapeDataString(request.Content)}");

        var url = "snippets/search" + (qs.Any() ? "?" + string.Join("&", qs) : "");
        return await GetFromJsonAsync<List<SnippetResponse>>(url);
    }

    // ── Share link ───────────────────────────────────────────────────────────

    /// <summary>Retrieves a snippet by its public share token (no auth required).</summary>
    public async Task<SnippetResponse?> GetSharedSnippetAsync(string token)
        => await GetFromJsonAsync<SnippetResponse>($"snippets/share/{token}");

    // ── Revoke ───────────────────────────────────────────────────────────────

    /// <summary>Revokes the share link. Returns (true, null) on success or (false, errorMessage).</summary>
    public async Task<(bool Success, string? Error)> RevokeSnippetAsync(int id)
    {
        var response = await PostAsync($"snippets/{id}/revoke", new { });
        if (response.IsSuccessStatusCode) return (true, null);

        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return (false, body?.Message ?? "Không thể thu hồi snippet.");
        }
        catch
        {
            return (false, "Không thể thu hồi snippet.");
        }
    }
    public async Task<(bool Success, string? Error)> RestoreSnippetAsync(int id)
    {
        var response = await PostAsync($"snippets/{id}/restore", new { });

        if (response.IsSuccessStatusCode)
            return (true, null);

        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return (false, body?.Message ?? "Không thể khôi phục link chia sẻ.");
        }
        catch
        {
            return (false, "Không thể khôi phục link chia sẻ.");
        }
    }
    private record ErrorBody(string? Message);
}
