namespace PingMe.Frontend.Models;

public class BlockResponse
{
    public int BlockedUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BlockStatusResponse
{
    public bool IsBlockedByMe { get; set; }
    public bool HasBlockedMe { get; set; }
    public bool IsBlocked { get; set; }
}
