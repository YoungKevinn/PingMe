using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("PasswordResetOtps")]
public class PasswordResetOtp
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(6)]
    public string OtpCode { get; set; } = "";   // 6 số
    
    [Required]
    public DateTime ExpiresAt { get; set; }      // UtcNow + 10 phút
    
    [Required]
    public bool IsUsed { get; set; } = false;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
