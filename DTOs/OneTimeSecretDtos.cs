namespace PingMe.DTOs;

public class CreateOneTimeSecretRequest
{
    public string? SecretText { get; set; }
    public string? ExpiresIn { get; set; }
}

public class CreateOneTimeSecretResponse
{
    public int Id { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ViewOneTimeSecretResponse
{
    public string SecretText { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ViewedAt { get; set; }
}

public class OneTimeSecretListItemDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public bool IsViewed { get; set; }
    public bool IsRevoked { get; set; }
    public int? ViewedByUserId { get; set; }
    public string? ViewedByDisplayName { get; set; }
    public string? ViewedByAvatarUrl { get; set; }
    public string? ViewedUserAgent { get; set; }
    public bool IsAnonymousViewed { get; set; }
}
