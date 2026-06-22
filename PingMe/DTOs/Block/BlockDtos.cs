namespace PingMe.DTOs.Block;

public class BlockResponse
{
    public int BlockedUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
