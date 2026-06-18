// Models/MessageEditHistory.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("MessageEditHistories")]
public class MessageEditHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MessageId { get; set; }

    [Required]
    [MaxLength(4000)]
    public string OldContent { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string NewContent { get; set; } = string.Empty;

    public DateTime EditedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; } = null!;
}