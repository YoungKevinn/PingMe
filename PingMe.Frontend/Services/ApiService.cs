using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace PingMe.Frontend.Services;

public class ApiService
{
    protected readonly HttpClient _http;
    protected readonly IJSRuntime _js;

    public const string BackendBaseUrl = "https://localhost:5001";
    protected const string BaseUrl = $"{BackendBaseUrl}/api";

    public ApiService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    protected async Task AddAuthHeaderAsync()
    {
        var token = await GetTokenAsync();

        _http.DefaultRequestHeaders.Authorization = null;

        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    protected async Task<HttpResponseMessage> GetAsync(string url)
    {
        await AddAuthHeaderAsync();
        return await _http.GetAsync(BuildUrl(url));
    }

    protected async Task<T?> GetFromJsonAsync<T>(string url)
    {
        try
        {
            await AddAuthHeaderAsync();

            var response = await _http.GetAsync(BuildUrl(url));

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            return default;
        }
    }

    protected async Task<HttpResponseMessage> PostAsync(string url, object data)
    {
        await AddAuthHeaderAsync();
        return await _http.PostAsJsonAsync(BuildUrl(url), data);
    }

    protected async Task<T?> PostAndGetJsonAsync<T>(string url, object data)
    {
        try
        {
            await AddAuthHeaderAsync();

            var response = await _http.PostAsJsonAsync(BuildUrl(url), data);

            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            return default;
        }
    }

    protected async Task<HttpResponseMessage> PutAsync(string url, object data)
    {
        await AddAuthHeaderAsync();
        return await _http.PutAsJsonAsync(BuildUrl(url), data);
    }

    protected async Task<HttpResponseMessage> PatchAsync(string url, object data)
    {
        await AddAuthHeaderAsync();

        var request = new HttpRequestMessage(HttpMethod.Patch, BuildUrl(url))
        {
            Content = JsonContent.Create(data)
        };

        return await _http.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        await AddAuthHeaderAsync();
        return await _http.DeleteAsync(BuildUrl(url));
    }

    private async Task<string?> GetTokenAsync()
    {
        var keys = new[]
        {
            "auth_token",
            "authToken",
            "token",
            "jwt",
            "accessToken"
        };

        foreach (var key in keys)
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", key);

            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        return null;
    }

    private static string BuildUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BaseUrl;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return $"{BaseUrl}/{url.TrimStart('/')}";
    }
}
