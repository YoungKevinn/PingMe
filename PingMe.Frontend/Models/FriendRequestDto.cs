namespace PingMe.Frontend.Models;

public class SendFriendRequestDto
{
    public int TargetUserId { get; set; }
}

public class FriendRequestDto
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public string FromUserName { get; set; } = string.Empty;
    public string? FromUserAvatar { get; set; }
    public int ToUserId { get; set; }
    public string ToUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
