using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class SessionService : ApiService
{
    public SessionService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<List<SessionResponse>?> GetSessionsAsync()
        => await GetFromJsonAsync<List<SessionResponse>>("sessions");

    public async Task<bool> RevokeSessionAsync(int sessionId)
    {
        var r = await DeleteAsync($"sessions/{sessionId}");
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> RevokeOtherSessionsAsync()
    {
        var r = await DeleteAsync("sessions/others");
        return r.IsSuccessStatusCode;
    }
}
