using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("BlockedUsers")]
public class BlockedUser
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BlockerUserId { get; set; }

    [Required]
    public int BlockedUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BlockerUserId))]
    public User Blocker { get; set; } = null!;

    [ForeignKey(nameof(BlockedUserId))]
    public User Blocked { get; set; } = null!;
}
