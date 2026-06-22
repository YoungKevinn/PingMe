using Microsoft.JSInterop;
using PingMe.Frontend.Models;
using System.Net.Http.Json;
using System.Web;

namespace PingMe.Frontend.Services;

public class IocService : ApiService
{
    public IocService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<IocResponse?> CreateAsync(CreateIocRequest request)
        => await PostAndGetJsonAsync<IocResponse>("iocs", request);

    public async Task<IocResponse?> CreateFromCommandAsync(CreateIocFromCommandRequest request)
        => await PostAndGetJsonAsync<IocResponse>("iocs/from-command", request);

    public async Task<List<IocResponse>> SearchAsync(IocSearchRequest request)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
            query["keyword"] = request.Keyword;

        if (!string.IsNullOrWhiteSpace(request.Type))
            query["type"] = request.Type;

        if (!string.IsNullOrWhiteSpace(request.Severity))
            query["severity"] = request.Severity;

        if (!string.IsNullOrWhiteSpace(request.Status))
            query["status"] = request.Status;

        if (request.GroupId.HasValue)
            query["groupId"] = request.GroupId.Value.ToString();

        if (request.PeerUserId.HasValue)
            query["peerUserId"] = request.PeerUserId.Value.ToString();

        var suffix = query.Count > 0 ? $"?{query}" : string.Empty;

        return await GetFromJsonAsync<List<IocResponse>>($"iocs{suffix}") ?? new();
    }

    public async Task<IocStatsResponse?> GetStatsAsync()
        => await GetFromJsonAsync<IocStatsResponse>("iocs/stats");

    public async Task<IocResponse?> UpdateAsync(int id, UpdateIocRequest request)
    {
        var response = await PutAsync($"iocs/{id}", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<IocResponse>();
    }

    public async Task<IocResponse?> UpdateStatusAsync(int id, string status)
    {
        var response = await PatchAsync($"iocs/{id}/status", new { status });

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<IocResponse>();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await DeleteAsync($"iocs/{id}");
        return response.IsSuccessStatusCode;
    }
}
