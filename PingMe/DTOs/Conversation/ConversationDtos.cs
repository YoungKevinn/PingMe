namespace PingMe.DTOs.Conversation;

public class ConversationResponse
{
    public string Type { get; set; } = string.Empty; // "dm" | "group"
    public int? PeerId { get; set; }
    public string? PeerDisplayName { get; set; }
    public string? PeerAvatarUrl { get; set; }
    public bool? PeerIsOnline { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? GroupAvatarUrl { get; set; }
    public int MemberCount { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsPinned { get; set; }
}

public class SetNicknameRequest
{
    public int TargetUserId { get; set; }
    public int? GroupId { get; set; }
    public string Nickname { get; set; } = string.Empty;
}

public class SetBackgroundRequest
{
    public int? PeerUserId { get; set; }
    public int? GroupId { get; set; }
    public string BackgroundType { get; set; } = "Color";
    public string BackgroundValue { get; set; } = string.Empty;
}
