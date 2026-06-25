using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PingMe.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);
    Task SendLoginAnomalyAlertAsync(string toEmail, string toName, string newIp, string oldIp);
    Task SendOtpEmailAsync(string toEmail, string displayName, string otpCode);
    Task SendNewPasswordEmailAsync(string toEmail, string displayName, string newPassword);
    Task SendEmailVerificationOtpAsync(string toEmail, string displayName, string otpCode);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly PingMe.Settings.EmailSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, Microsoft.Extensions.Options.IOptions<PingMe.Settings.EmailSettings> settings, IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _logger = logger;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        var baseUrl = ctx is not null
            ? $"{ctx.Request.Scheme}://{ctx.Request.Host}"
            : "http://localhost:5001";

        var resetUrl = $"{baseUrl}/auth/reset-password?token={resetToken}";

        var body = $"""
            <div style="font-family:'Inter',sans-serif;max-width:520px;margin:auto;color:#1e293b;">
              <div style="background:linear-gradient(135deg,#1D4ED8,#2563EB);padding:28px 32px;border-radius:12px 12px 0 0;">
                <h2 style="margin:0;color:white;font-size:1.3rem;font-weight:800;">🔐 PingMe — Đặt lại mật khẩu</h2>
              </div>
              <div style="background:#ffffff;padding:28px 32px;border:1px solid #e2e8f0;border-top:none;">
                <p>Xin chào <strong>{toName}</strong>,</p>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>{toEmail}</strong>.</p>
                <p>Bấm vào nút bên dưới để đặt mật khẩu mới:</p>
                <div style="text-align:center;margin:28px 0;">
                  <a href="{resetUrl}"
                     style="background:#2563EB;color:#fff;padding:14px 32px;border-radius:8px;
                            text-decoration:none;font-weight:700;font-size:1rem;display:inline-block;">
                    Đặt lại mật khẩu
                  </a>
                </div>
                <p style="color:#64748b;font-size:0.85rem;">
                  Link có hiệu lực trong <strong>1 giờ</strong>.<br/>
                  Nếu bạn không yêu cầu, hãy bỏ qua email này — tài khoản vẫn an toàn.
                </p>
                <hr style="border:none;border-top:1px solid #e2e8f0;margin:20px 0;"/>
                <p style="color:#94a3b8;font-size:0.78rem;margin:0;">
                  PingMe — DevSecOps Collaboration Platform<br/>
                  Đừng trả lời email này.
                </p>
              </div>
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
            message.From.Add(new MailboxAddress("PingMe", _settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body    = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send email to {Email}: {Error}", toEmail, ex.Message);
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

    public async Task SendEmailVerificationOtpAsync(string toEmail, string displayName, string otpCode)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PingMe", _settings.SenderEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = $"[PingMe] Xác minh email của bạn: {otpCode}";

            message.Body = new TextPart("html")
            {
                Text = $@"
<div style='font-family:sans-serif;max-width:480px;margin:auto;'>
  <h2 style='color:#2563eb;'>PingMe</h2>
  <p>Xin chào <b>{displayName}</b>,</p>
  <p>Cảm ơn bạn đã đăng ký! Hãy nhập mã OTP dưới đây để xác minh địa chỉ email và kích hoạt tài khoản:</p>
  <div style='font-size:36px;font-weight:bold;letter-spacing:8px;
              color:#1e293b;background:#f1f5f9;padding:20px;
              text-align:center;border-radius:8px;margin:24px 0;'>
    {otpCode}
  </div>
  <p>Mã có hiệu lực trong <b>10 phút</b>.</p>
  <p>Nếu bạn không thực hiện đăng ký này, hãy bỏ qua email này.</p>
  <hr style='border:none;border-top:1px solid #e2e8f0;margin:24px 0;'/>
  <p style='color:#94a3b8;font-size:12px;'>PingMe — DevSecOps Collaboration Platform</p>
</div>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to send verification OTP to {Email}: {Error}", toEmail, ex.Message);
        }
    }

    public async Task SendNewPasswordEmailAsync(string toEmail, string displayName, string newPassword)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PingMe", _settings.SenderEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = "[PingMe] Mật khẩu mới của bạn";

            message.Body = new TextPart("html")
            {
                Text = $@"
<div style='font-family:sans-serif;max-width:480px;margin:auto;'>
  <h2 style='color:#2563eb;'>PingMe</h2>
  <p>Xin chào <b>{displayName}</b>,</p>
  <p>Mật khẩu của bạn đã được đặt lại. Đây là mật khẩu mới để đăng nhập:</p>
  <div style='font-size:24px;font-weight:bold;letter-spacing:2px;
              color:#1e293b;background:#f1f5f9;padding:20px;
              text-align:center;border-radius:8px;margin:24px 0;
              font-family:monospace;'>
    {newPassword}
  </div>
  <p>Vì lý do bảo mật, hãy <b>đăng nhập và đổi mật khẩu</b> ngay sau khi vào lại tài khoản.</p>
  <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy liên hệ quản trị viên ngay.</p>
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
            _logger.LogWarning("Failed to send new-password email to {Email}: {Error}", toEmail, ex.Message);
        }
    }
}
