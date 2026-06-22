using PingMe.DTOs.Poll;

namespace PingMe.DTOs.Message;

public class SendMessageRequest
{
    public int? ReceiverId { get; set; }   // DM
    public int? GroupId { get; set; }      // Group
    public string? Content { get; set; }
    public string MessageType { get; set; } = "Text";
    public int? ReplyToMessageId { get; set; }
    public int? ForwardedFromMessageId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class EditMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class MessageResponse
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public int? GroupId { get; set; }
    public int? ReceiverId { get; set; }
    public string? Content { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public int? ReplyToMessageId { get; set; }
    public MessageResponse? ReplyToMessage { get; set; }
    public int? ForwardedFromMessageId { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsEdited { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AttachmentResponse> Attachments { get; set; } = [];
    public List<ReactionSummary> Reactions { get; set; } = [];
    public List<int> ReadByUserIds { get; set; } = [];
    public PollResponse? Poll { get; set; }
}

public class AttachmentResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
}

public class ReactionSummary
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<int> UserIds { get; set; } = [];
}

public class GetMessagesRequest
{
    public int? Before { get; set; }  // Message ID cursor for infinite scroll
    public int Limit { get; set; } = 30;
}

public class PinMessageRequest
{
    public bool IsPinned { get; set; }
}
public class ConversationAttachmentResponse
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;

    public int? GroupId { get; set; }
    public int? ReceiverId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public bool IsImage { get; set; }

    public DateTime CreatedAt { get; set; }
}