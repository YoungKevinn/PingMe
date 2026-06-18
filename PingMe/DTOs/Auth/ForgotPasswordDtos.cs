namespace PingMe.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = "";
}

public class VerifyOtpDto
{
    public string Email { get; set; } = "";
    public string OtpCode { get; set; } = "";
}

public class ResetPasswordDto
{
    public string Email { get; set; } = "";
    public string OtpCode { get; set; } = "";
    public string NewPassword { get; set; } = ""; // min 8 ký tự
}
