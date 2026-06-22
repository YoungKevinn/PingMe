using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs;
using PingMe.Models;

namespace PingMe.Services;

public interface IOneTimeSecretService
{
    Task<(CreateOneTimeSecretResponse? Secret, string? Error)> CreateAsync(
        int currentUserId,
        CreateOneTimeSecretRequest request,
        string baseUrl);

    Task<(ViewOneTimeSecretResponse? Secret, string? Error)> ViewAsync(
        string rawToken,
        int? viewerUserId,
        string? ipAddress,
        string? userAgent);
    Task<(bool Success, string? Error)> RevokeAsync(int currentUserId, int id);
    Task<List<OneTimeSecretListItemDto>> GetMineAsync(int currentUserId);
}

public class OneTimeSecretService : IOneTimeSecretService
{
    private const int MaxSecretLength = 8000;
    private readonly AppDbContext _db;
    private readonly IDataProtector _protector;

    public OneTimeSecretService(AppDbContext db, IDataProtectionProvider dataProtectionProvider)
    {
        _db = db;
        _protector = dataProtectionProvider.CreateProtector("PingMe.OneTimeSecrets.v1");
    }

    public async Task<(CreateOneTimeSecretResponse? Secret, string? Error)> CreateAsync(
        int currentUserId,
        CreateOneTimeSecretRequest request,
        string baseUrl)
    {
        var secretText = request.SecretText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(secretText))
            return (null, "Secret không được để trống.");

        if (secretText.Length > MaxSecretLength)
            return (null, $"Secret không được vượt quá {MaxSecretLength} ký tự.");

        if (!TryGetExpiration(request.ExpiresIn, out var expiresAt))
            return (null, "Thời gian hết hạn không hợp lệ. Chỉ hỗ trợ 5m, 1h hoặc 1d.");

        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);

        var entity = new OneTimeSecret
        {
            TokenHash = tokenHash,
            SecretCipherText = _protector.Protect(secretText),
            CreatedByUserId = currentUserId,
            ExpiresAt = expiresAt,
            IsViewed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.OneTimeSecrets.Add(entity);
        await _db.SaveChangesAsync();

        var shareUrl = $"{baseUrl.TrimEnd('/')}/secret/{Uri.EscapeDataString(rawToken)}";

        return (new CreateOneTimeSecretResponse
        {
            Id = entity.Id,
            ShareUrl = shareUrl,
            ExpiresAt = entity.ExpiresAt
        }, null);
    }

    public async Task<(ViewOneTimeSecretResponse? Secret, string? Error)> ViewAsync(
        string rawToken,
        int? viewerUserId,
        string? ipAddress,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return (null, "Token không hợp lệ.");

        var tokenHash = HashToken(rawToken);
        var now = DateTime.UtcNow;

        var secret = await _db.OneTimeSecrets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

        if (secret is null)
            return (null, "Secret không tồn tại.");

        if (secret.IsRevoked)
            return (null, "Secret đã bị thu hồi.");

        if (secret.IsViewed)
            return (null, "Secret đã được xem trước đó.");

        if (secret.ExpiresAt <= now)
            return (null, "Secret đã hết hạn.");

        string plainText;
        try
        {
            plainText = _protector.Unprotect(secret.SecretCipherText);
        }
        catch (CryptographicException)
        {
            return (null, "Không thể giải mã secret.");
        }

        var viewedAt = DateTime.UtcNow;
        var viewedIpHash = string.IsNullOrWhiteSpace(ipAddress) ? null : HashToken(ipAddress);
        var viewedUserAgent = NormalizeUserAgent(userAgent);

        var updated = await _db.OneTimeSecrets
            .Where(s =>
                s.Id == secret.Id &&
                !s.IsViewed &&
                !s.IsRevoked &&
                s.ExpiresAt > viewedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsViewed, true)
                .SetProperty(s => s.ViewedAt, viewedAt)
                .SetProperty(s => s.ViewedByUserId, viewerUserId)
                .SetProperty(s => s.ViewedIpHash, viewedIpHash)
                .SetProperty(s => s.ViewedUserAgent, viewedUserAgent));

        if (updated == 0)
            return (null, "Secret đã hết hạn hoặc đã được xem.");

        return (new ViewOneTimeSecretResponse
        {
            SecretText = plainText,
            ExpiresAt = secret.ExpiresAt,
            ViewedAt = viewedAt
        }, null);
    }

    public async Task<(bool Success, string? Error)> RevokeAsync(int currentUserId, int id)
    {
        var secret = await _db.OneTimeSecrets.FirstOrDefaultAsync(s => s.Id == id);

        if (secret is null)
            return (false, "Secret không tồn tại.");

        if (secret.CreatedByUserId != currentUserId)
            return (false, "Không có quyền thu hồi secret này.");

        if (secret.IsViewed)
            return (false, "Secret đã được xem nên không thể thu hồi.");

        if (secret.IsRevoked)
            return (true, null);

        secret.IsRevoked = true;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<List<OneTimeSecretListItemDto>> GetMineAsync(int currentUserId)
    {
        return await _db.OneTimeSecrets
            .AsNoTracking()
            .Where(s => s.CreatedByUserId == currentUserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new OneTimeSecretListItemDto
            {
                Id = s.Id,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                ViewedAt = s.ViewedAt,
                IsViewed = s.IsViewed,
                IsRevoked = s.IsRevoked,
                ViewedByUserId = s.ViewedByUserId,
                ViewedByDisplayName = s.ViewedByUser != null ? s.ViewedByUser.DisplayName : null,
                ViewedByAvatarUrl = s.ViewedByUser != null ? s.ViewedByUser.AvatarUrl : null,
                ViewedUserAgent = s.ViewedUserAgent,
                IsAnonymousViewed = s.IsViewed && s.ViewedByUserId == null
            })
            .ToListAsync();
    }

    private static bool TryGetExpiration(string? expiresIn, out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow;

        var duration = expiresIn?.Trim().ToLowerInvariant();
        var offset = duration switch
        {
            "5m" => TimeSpan.FromMinutes(5),
            "1h" => TimeSpan.FromHours(1),
            "1d" => TimeSpan.FromDays(1),
            _ => (TimeSpan?)null
        };

        if (offset is null)
            return false;

        expiresAt = DateTime.UtcNow.Add(offset.Value);
        return true;
    }

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    private static string? NormalizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        var normalized = userAgent.Trim();
        return normalized.Length <= 512 ? normalized : normalized[..512];
    }
}
