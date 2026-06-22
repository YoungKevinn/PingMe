using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    [MaxLength(150)]
    public string? WorkLocation { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public bool IsOnline { get; set; } = false;

    public DateTime? LastSeen { get; set; }

    [MaxLength(45)]   // IPv6 max length
    public string? LastLoginIp { get; set; }

    [MaxLength(255)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    public bool IsEmailVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<GroupMember> GroupMemberships { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
    public ICollection<Message> ReceivedMessages { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
    public ICollection<SavedMessage> SavedMessages { get; set; } = [];
    public ICollection<MessageReadReceipt> ReadReceipts { get; set; } = [];
    public ICollection<BlockedUser> BlockedUsers { get; set; } = [];
    public ICollection<BlockedUser> BlockedByUsers { get; set; } = [];
    public ICollection<PinnedConversation> PinnedConversations { get; set; } = [];
    public ICollection<CodeSnippet> CodeSnippets { get; set; } = [];
    public ICollection<UserSession> Sessions { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
    public ICollection<ConversationNickname> NicknamesSet { get; set; } = [];
    public ICollection<ConversationNickname> NicknamesReceived { get; set; } = [];
    public ICollection<ConversationBackground> ConversationBackgrounds { get; set; } = [];
    public ICollection<OneTimeSecret> OneTimeSecrets { get; set; } = [];
    public ICollection<GroupTask> CreatedTasks { get; set; } = [];
    public ICollection<GroupTask> AssignedTasks { get; set; } = [];
}
