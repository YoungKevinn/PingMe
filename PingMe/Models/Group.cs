using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("Groups")]
public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedBy { get; set; } = null!;

    public ICollection<GroupMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Webhook> Webhooks { get; set; } = [];
    public ICollection<PinnedConversation> PinnedByUsers { get; set; } = [];
    public ICollection<ConversationNickname> Nicknames { get; set; } = [];
    public ICollection<ConversationBackground> Backgrounds { get; set; } = [];
    public ICollection<GroupTask> Tasks { get; set; } = [];
}
