namespace PingMe.Frontend.Models;

public class SavedMessageDto
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string? Content { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime SavedAt { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? PeerUserId { get; set; }
    public string? PeerDisplayName { get; set; }
    public string ConversationType { get; set; } = "DM";
}

public class SavedMessageFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public string? Type { get; set; }
    public int? GroupId { get; set; }
    public int? PeerUserId { get; set; }
}

public class CheckSavedMessagesRequest
{
    public List<int> MessageIds { get; set; } = new();
}
