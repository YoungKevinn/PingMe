using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

/// <summary>
/// Stores per-user chat background settings.
/// PeerUserId set = DM background. GroupId set = group background.
/// Exactly one of PeerUserId or GroupId must be non-null.
/// </summary>
[Table("ConversationBackgrounds")]
public class ConversationBackground
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // The user who set this background
    [Required]
    public int UserId { get; set; }

    // DM context: the other participant
    public int? PeerUserId { get; set; }

    // Group context
    public int? GroupId { get; set; }

    [Required]
    public BackgroundType BackgroundType { get; set; } = BackgroundType.Color;

    // Hex color (#RRGGBB), image URL, or gradient CSS string
    [Required]
    [MaxLength(500)]
    public string BackgroundValue { get; set; } = "#FFFFFF";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(PeerUserId))]
    public User? PeerUser { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }
}
