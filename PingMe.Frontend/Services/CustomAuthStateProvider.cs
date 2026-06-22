using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace PingMe.Frontend.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;
    private readonly AuthService _authService;

    public CustomAuthStateProvider(IJSRuntime js, HttpClient http, AuthService authService)
    {
        _js = js;
        _http = http;
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "auth_token");

            if (string.IsNullOrWhiteSpace(token))
                return Anonymous();

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var user = await _authService.GetCurrentUserAsync();

            if (user == null)
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
                _http.DefaultRequestHeaders.Authorization = null;
                return Anonymous();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("DisplayName", user.DisplayName ?? string.Empty),
                new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
            _http.DefaultRequestHeaders.Authorization = null;
            return Anonymous();
        }
    }

    public void NotifyUserLoggedIn()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async void NotifyUserLoggedOut()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    private static AuthenticationState Anonymous()
    {
        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity())
        );
    }
}