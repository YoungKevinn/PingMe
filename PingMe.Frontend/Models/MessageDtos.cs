namespace PingMe.Frontend.Models;

public class SendMessageRequest
{
    public int? ReceiverId { get; set; }
    public int? GroupId { get; set; }
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
    public bool IsSaved { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<AttachmentResponse> Attachments { get; set; } = new();
    public List<ReactionSummary> Reactions { get; set; } = new();
    public List<int> ReadByUserIds { get; set; } = new();

    public bool IsRead => ReadByUserIds.Count > 0;

    public bool HasCodeBlock =>
        !string.IsNullOrWhiteSpace(Content) &&
        Content.Contains("```");

    public string CodeLanguage
    {
        get
        {
            if (!HasCodeBlock || Content is null)
                return string.Empty;

            var start = Content.IndexOf("```", StringComparison.Ordinal);
            var lineEnd = Content.IndexOf('\n', start + 3);

            if (lineEnd < 0)
                return string.Empty;

            return Content[(start + 3)..lineEnd].Trim();
        }
    }

    public string CodeContent
    {
        get
        {
            if (!HasCodeBlock || Content is null)
                return Content ?? string.Empty;

            var start = Content.IndexOf("```", StringComparison.Ordinal);
            var codeStart = Content.IndexOf('\n', start + 3);

            if (codeStart < 0)
                return Content;

            codeStart++;

            var end = Content.IndexOf("```", codeStart, StringComparison.Ordinal);
            if (end < 0)
                end = Content.Length;

            return Content[codeStart..end].TrimEnd();
        }
    }
}

public class AttachmentResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;

    public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public bool IsAudio => MimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

    public string DisplaySize
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024d:0.#} KB";
            return $"{FileSize / 1024d / 1024d:0.##} MB";
        }
    }
}

public class ReactionSummary
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<int> UserIds { get; set; } = new();
}

public class ReactionUpdateEvent
{
    public int MessageId { get; set; }
    public int? GroupId { get; set; }
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; }
    public List<ReactionSummary> Reactions { get; set; } = new();
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
    public bool IsAudio => MimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

    public DateTime CreatedAt { get; set; }

    public string DisplaySize
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024d:0.#} KB";
            return $"{FileSize / 1024d / 1024d:0.##} MB";
        }
    }
}

public class ReadReceiptEvent
{
    public int? MessageId { get; set; }
    public List<int> MessageIds { get; set; } = new();
    public int UserId { get; set; }
}

public class MessagePinnedEvent
{
    public int MessageId { get; set; }
    public bool IsPinned { get; set; }
    public int? GroupId { get; set; }
    public int? ReceiverId { get; set; }
    public int SenderId { get; set; }
    public MessageResponse? Message { get; set; }
}
