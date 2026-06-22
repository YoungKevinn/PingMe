using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("Messages")]
public class Message
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SenderId { get; set; }

    // Nullable: null = DM, has value = group message
    public int? GroupId { get; set; }

    // Nullable: null = group message, has value = DM
    public int? ReceiverId { get; set; }

    [MaxLength(4000)]
    public string? Content { get; set; }

    [Required]
    public MessageType MessageType { get; set; } = MessageType.Text;

    // Self-reference for reply threads
    public int? ReplyToMessageId { get; set; }

    // Track forwarded origin
    public int? ForwardedFromMessageId { get; set; }

    public bool IsDeleted { get; set; } = false;

    public bool IsEdited { get; set; } = false;

    public bool IsPinned { get; set; } = false;

    // TTL support (Phase 6 - Message expiry)
    public DateTime? ExpiresAt { get; set; }

    // Phase 2: linked snippet (for MessageType.Snippet messages)
    public int? SnippetId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SenderId))]
    public User Sender { get; set; } = null!;

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    public User? Receiver { get; set; }

    [ForeignKey(nameof(ReplyToMessageId))]
    public Message? ReplyToMessage { get; set; }

    [ForeignKey(nameof(ForwardedFromMessageId))]
    public Message? ForwardedFromMessage { get; set; }

    [ForeignKey(nameof(SnippetId))]
    public CodeSnippet? Snippet { get; set; }

    public ICollection<MessageAttachment> Attachments { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
    public ICollection<SavedMessage> SavedMessages { get; set; } = [];
    public ICollection<MessageReadReceipt> ReadReceipts { get; set; } = [];
    public ICollection<CodeSnippet> CodeSnippets { get; set; } = [];
    public Poll? Poll { get; set; }
}
