using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("GroupTasks")]
public class GroupTask
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Open";

    public DateTime? DueAtUtc { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    public int? AssignedToUserId { get; set; }

    public int? SourceMessageId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group Group { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedByUser { get; set; } = null!;

    [ForeignKey(nameof(AssignedToUserId))]
    public User? AssignedToUser { get; set; }

    [ForeignKey(nameof(SourceMessageId))]
    public Message? SourceMessage { get; set; }
}
