using PingMe.Frontend.Services;

namespace PingMe.Frontend.Helpers;

public static class UrlHelper
{
    public static string? BuildBackendUrl(string? url, long? version = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var result = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{ApiService.BackendBaseUrl}/{url.TrimStart('/')}";

        if (!version.HasValue)
            return result;

        var separator = result.Contains('?') ? "&" : "?";
        return $"{result}{separator}v={version.Value}";
    }
}
