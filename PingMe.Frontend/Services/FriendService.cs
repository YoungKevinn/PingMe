using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;

namespace PingMe.Frontend.Services
{
    public class FriendService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private const string ApiBase = "http://localhost:5000/api/";

        public FriendService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        private async Task AddAuthHeaderAsync()
        {
            try
            {
                var token = await _js.InvokeAsync<string>("localStorage.getItem", "auth_token");
                _http.DefaultRequestHeaders.Authorization = null;
                if (!string.IsNullOrWhiteSpace(token))
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            catch
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<bool> SendRequestAsync(int targetUserId)
        {
            await AddAuthHeaderAsync();
            var response = await _http.PostAsJsonAsync(ApiBase + "friends/request", new { targetUserId });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RespondRequestAsync(int requestId, bool accept)
        {
            await AddAuthHeaderAsync();
            var url = ApiBase + $"friends/request/{requestId}/respond?accept={accept}";
            var response = await _http.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<object>?> GetIncomingAsync()
        {
            await AddAuthHeaderAsync();
            return await _http.GetFromJsonAsync<List<object>>(ApiBase + "friends/requests/incoming");
        }

        public async Task<List<object>?> GetOutgoingAsync()
        {
            await AddAuthHeaderAsync();
            return await _http.GetFromJsonAsync<List<object>>(ApiBase + "friends/requests/outgoing");
        }
    }
}
