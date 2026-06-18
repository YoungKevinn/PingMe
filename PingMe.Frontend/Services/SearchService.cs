using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class SearchService : ApiService
{
    public SearchService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<GlobalSearchResponse?> GlobalSearchAsync(GlobalSearchRequest request)
    {
        var query = new List<string>
        {
            $"keyword={Uri.EscapeDataString(request.Keyword ?? string.Empty)}",
            $"type={Uri.EscapeDataString(request.Type ?? "all")}",
            $"page={request.Page}",
            $"pageSize={request.PageSize}"
        };

        if (request.FromDate.HasValue)
            query.Add($"fromDate={Uri.EscapeDataString(request.FromDate.Value.ToString("O"))}");

        if (request.ToDate.HasValue)
            query.Add($"toDate={Uri.EscapeDataString(request.ToDate.Value.ToString("O"))}");

        if (request.SenderId.HasValue)
            query.Add($"senderId={request.SenderId.Value}");

        if (request.GroupId.HasValue)
            query.Add($"groupId={request.GroupId.Value}");

        if (request.PeerUserId.HasValue)
            query.Add($"peerUserId={request.PeerUserId.Value}");

        if (!string.IsNullOrWhiteSpace(request.Severity))
            query.Add($"severity={Uri.EscapeDataString(request.Severity)}");

        return await GetFromJsonAsync<GlobalSearchResponse>($"search/global?{string.Join("&", query)}");
    }

    public async Task<SearchResultResponse?> SearchAsync(string query, int limit = 20)
        => await GetFromJsonAsync<SearchResultResponse>($"search?q={Uri.EscapeDataString(query)}&limit={limit}");

    public async Task<List<MessageSearchResult>?> SearchMessagesInConversationAsync(
        string query,
        int? peerId,
        int? groupId,
        int limit = 20)
    {
        var url = $"search/messages?q={Uri.EscapeDataString(query)}&limit={limit}";

        if (peerId.HasValue)
            url += $"&peerId={peerId.Value}";
        else if (groupId.HasValue)
            url += $"&groupId={groupId.Value}";

        return await GetFromJsonAsync<List<MessageSearchResult>>(url);
    }
}
