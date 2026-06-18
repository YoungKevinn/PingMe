using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

public enum FriendshipStatus
{
    Pending,
    Accepted,
    Declined,
    Blocked
}

[Table("Friendships")]
public class Friendship
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserAId { get; set; }  // người chủ động (có thể là người gửi lời mời)

    [Required]
    public int UserBId { get; set; }  // người kia

    [Required]
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(UserAId))]
    public User UserA { get; set; } = null!;

    [ForeignKey(nameof(UserBId))]
    public User UserB { get; set; } = null!;
}