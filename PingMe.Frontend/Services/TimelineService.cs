using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class TimelineService : ApiService
{
    public TimelineService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<List<TimelineEventDto>> GetGroupTimelineAsync(int groupId, TimelineQueryDto query)
    {
        var url = $"groups/{groupId}/timeline?page={query.Page}&pageSize={query.PageSize}";
        if (!string.IsNullOrEmpty(query.EventType)) url += $"&eventType={query.EventType}";
        if (!string.IsNullOrEmpty(query.SearchTerm)) url += $"&searchTerm={System.Uri.EscapeDataString(query.SearchTerm)}";
        if (query.From.HasValue) url += $"&from={System.Uri.EscapeDataString(query.From.Value.ToString("O"))}";
        if (query.To.HasValue) url += $"&to={System.Uri.EscapeDataString(query.To.Value.ToString("O"))}";

        return await GetFromJsonAsync<List<TimelineEventDto>>(url) ?? new List<TimelineEventDto>();
    }
}
