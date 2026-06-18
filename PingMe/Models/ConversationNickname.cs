using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

/// <summary>
/// Stores custom nicknames a user sets for another user.
/// In DM: SetByUserId sets a nickname for TargetUserId (GroupId = null).
/// In Group: same but scoped to a specific group.
/// Each user sees their own nickname mapping independently.
/// </summary>
[Table("ConversationNicknames")]
public class ConversationNickname
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Who set this nickname
    [Required]
    public int SetByUserId { get; set; }

    // Whose nickname is being set
    [Required]
    public int TargetUserId { get; set; }

    // Null = DM context, has value = group context
    public int? GroupId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nickname { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SetByUserId))]
    public User SetBy { get; set; } = null!;

    [ForeignKey(nameof(TargetUserId))]
    public User Target { get; set; } = null!;

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }
}
