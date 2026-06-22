using Microsoft.JSInterop;
using PingMe.Frontend.Models;
using System.Net.Http.Json;

namespace PingMe.Frontend.Services;

public class AuditLogService : ApiService
{
    public AuditLogService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<List<AuditLogResponse>> GetLogsAsync(int? userId = null, string? action = null, string? ip = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20)
    {
        var url = $"audit-logs?page={page}&pageSize={pageSize}";
        if (userId.HasValue) url += $"&userId={userId}";
        if (!string.IsNullOrEmpty(action)) url += $"&action={Uri.EscapeDataString(action)}";
        if (!string.IsNullOrEmpty(ip)) url += $"&ip={ip}";
        if (from.HasValue) url += $"&from={from.Value:O}";
        if (to.HasValue) url += $"&to={to.Value:O}";
        return await GetFromJsonAsync<List<AuditLogResponse>>(url) ?? new();
    }

    public async Task<AuditLogResponse?> GetLogByIdAsync(int id)
    {
        return await GetFromJsonAsync<AuditLogResponse>($"audit-logs/{id}");
    }

    public async Task<List<AuditLogResponse>> GetMyLogsAsync(string? action = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20)
    {
        var url = $"audit-logs/me?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(action)) url += $"&action={Uri.EscapeDataString(action)}";
        if (from.HasValue) url += $"&from={from.Value:O}";
        if (to.HasValue) url += $"&to={to.Value:O}";
        return await GetFromJsonAsync<List<AuditLogResponse>>(url) ?? new();
    }
}