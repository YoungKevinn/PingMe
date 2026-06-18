using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

/// <summary>
/// Tracks which conversations a user has pinned to the top of their list.
/// Exactly one of PeerUserId or GroupId must be non-null.
/// </summary>
[Table("PinnedConversations")]
public class PinnedConversation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    // DM: the other participant
    public int? PeerUserId { get; set; }

    // Group conversation
    public int? GroupId { get; set; }

    public DateTime PinnedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(PeerUserId))]
    public User? PeerUser { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }
}
