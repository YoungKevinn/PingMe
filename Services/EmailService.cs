using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PingMe.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);
    Task SendLoginAnomalyAlertAsync(string toEmail, string toName, string newIp, string oldIp);
    Task SendOtpEmailAsync(string toEmail, string displayName, string otpCode);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly PingMe.Settings.EmailSettings _settings;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, Microsoft.Extensions.Options.IOptions<PingMe.Settings.EmailSettings> settings)
    {
        _config = config;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
    {
        var resetUrl = $"http://localhost:5173/reset-password?token={resetToken}";

        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto">
              <h2 style="color:#534AB7">🔐 Đặt lại mật khẩu PingMe</h2>
              <p>Xin chào <strong>{toName}</strong>,</p>
              <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
              <p>
                <a href="{resetUrl}"
                   style="background:#534AB7;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block">
                  Đặt lại mật khẩu
                </a>
              </p>
              <p style="color:#888;font-size:13px">Link có hiệu lực trong <strong>1 giờ</strong>. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
            </div>
            """;

        await SendEmailAsync(toEmail, toName, "Đặt lại mật khẩu PingMe", body);
    }

    public async Task SendLoginAnomalyAlertAsync(string toEmail, string toName, string newIp, string oldIp)
    {
        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto">
              <h2 style="color:#D85A30">⚠️ Cảnh báo đăng nhập bất thường</h2>
              <p>Xin chào <strong>{toName}</strong>,</p>
              <p>Tài khoản PingMe của bạn vừa được đăng nhập từ một địa chỉ IP mới:</p>
              <table style="border-collapse:collapse;width:100%">
                <tr><td style="padding:8px;color:#888">IP cũ</td><td style="padding:8px"><code>{oldIp}</code></td></tr>
                <tr style="background:#f9f9f9"><td style="padding:8px;color:#888">IP mới</td><td style="padding:8px"><code>{newIp}</code></td></tr>
                <tr><td style="padding:8px;color:#888">Thời gian</td><td style="padding:8px">{DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</td></tr>
              </table>
              <p>Nếu đây không phải bạn, hãy đổi mật khẩu ngay lập tức.</p>
            </div>
            """;

        await SendEmailAsync(toEmail, toName, "⚠️ Cảnh báo đăng nhập bất thường - PingMe", body);
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:FromName"]!,
                _config["Email:FromAddress"]!));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body    = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["Email:SmtpHost"]!,
                int.Parse(_config["Email:SmtpPort"]!),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _config["Email:Username"]!,
                _config["Email:Password"]!);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Log lỗi nhưng không throw — không để email fail chặn flow chính
            _logger.LogWarning("Failed to send email to {Email}: {Error}", toEmail, ex.Message);
        }
    }

    public async Task SendOtpEmailAsync(string toEmail, string displayName, string otpCode)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PingMe", _settings.SenderEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = $"[PingMe] Mã xác nhận đặt lại mật khẩu: {otpCode}";

            message.Body = new TextPart("html")
            {
                Text = $@"
<div style='font-family:sans-serif;max-width:480px;margin:auto;'>
  <h2 style='color:#2563eb;'>PingMe</h2>
  <p>Xin chào <b>{displayName}</b>,</p>
  <p>Bạn đã yêu cầu đặt lại mật khẩu. Đây là mã OTP của bạn:</p>
  <div style='font-size:36px;font-weight:bold;letter-spacing:8px;
              color:#1e293b;background:#f1f5f9;padding:20px;
              text-align:center;border-radius:8px;margin:24px 0;'>
    {otpCode}
  </div>
  <p>Mã có hiệu lực trong <b>10 phút</b>.</p>
  <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
  <hr style='border:none;border-top:1px solid #e2e8f0;margin:24px 0;'/>
  <p style='color:#94a3b8;font-size:12px;'>PingMe — DevSecOps Collaboration Platform</p>
</div>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _settings.SmtpHost, _settings.SmtpPort,
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _settings.SenderEmail, _settings.SenderPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to send OTP email to {Email}: {Error}", toEmail, ex.Message);
        }
    }
}
