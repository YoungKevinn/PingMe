using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("ChatReminders")]
public class ChatReminder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    public DateTime RemindAtUtc { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    public bool IsSent { get; set; }

    public int CreatedByUserId { get; set; }

    public int? GroupId { get; set; }

    public int? PeerUserId { get; set; }

    public int? SourceMessageId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedByUser { get; set; } = null!;

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [ForeignKey(nameof(PeerUserId))]
    public User? PeerUser { get; set; }

    [ForeignKey(nameof(SourceMessageId))]
    public Message? SourceMessage { get; set; }
}
