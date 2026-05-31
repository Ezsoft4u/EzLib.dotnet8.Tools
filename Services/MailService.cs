using EzLib.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Serilog; // 新增：Serilog
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzLib.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger _logger; // Serilog logger
        public MailService(MailSettings mailSettings, ILogger? logger = null)
        {
            _mailSettings = mailSettings ?? throw new ArgumentNullException(nameof(mailSettings));
            _logger = logger ?? Log.ForContext<MailService>();
        }

        public async Task<MailResult> SendEmailAsync(MailRequest mailRequest)
        {
            var result = new MailResult();

            try
            {
                if (mailRequest == null)
                    throw new ArgumentNullException(nameof(mailRequest));
                if (string.IsNullOrWhiteSpace(mailRequest.ToEmail))
                    throw new ArgumentException("ToEmail 不可為空", nameof(mailRequest.ToEmail));

                LogDebug("開始建立郵件內容...");
                var email = CreateEmailMessage(mailRequest);
                var sslOption = GetSecureSocketOptions(_mailSettings.SSL);
                var isAuth = _mailSettings.IsAuth;

                LogDebug($"SMTP 設定 Host={_mailSettings.Host}, Port={_mailSettings.Port}, SSL={_mailSettings.SSL}, Auth={isAuth}");

                using var smtp = _mailSettings.Debug
                    ? new SmtpClient(new SerilogProtocolLogger(_logger, LogEventLevel.Information))
                    : new SmtpClient();

                if (_mailSettings.Debug)
                {
                    // 忽略憑證（僅 Debug）
                    smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtp.CheckCertificateRevocation = false;
                }

                await SendEmailAsync(smtp, email, sslOption, isAuth);

                result.IsSuccess = true;
                result.Message = "Email sent successfully.";
                LogDebug("郵件發送完成");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = BuildErrorMessage(ex);
                LogError("郵件發送失敗", ex);
            }

            return result;
        }

        internal sealed class SerilogProtocolLogger : IProtocolLogger
        {
            private readonly ILogger _logger;
            private readonly LogEventLevel _level;
            private readonly Encoding _encoding;

            public SerilogProtocolLogger(ILogger logger, LogEventLevel level = LogEventLevel.Information, Encoding? encoding = null)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _level = level;
                _encoding = encoding ?? Encoding.UTF8;
            }

            public IAuthenticationSecretDetector? AuthenticationSecretDetector { get; set; }

            public void LogConnect(Uri uri) => _logger.Write(_level, "SMTP Connect: {Uri}", uri);
            public void LogDisconnect(Uri uri) => _logger.Write(_level, "SMTP Disconnect: {Uri}", uri);

            public void LogClient(byte[] buffer, int offset, int count) => LogChunk("C", buffer, offset, count);
            public void LogServer(byte[] buffer, int offset, int count) => LogChunk("S", buffer, offset, count);

            private void LogChunk(string direction, byte[] buffer, int offset, int count)
            {
                if (count <= 0) return;
                var text = _encoding.GetString(buffer, offset, count).TrimEnd('\r', '\n');
                
                // 分行輸出，避免多行訊息被截斷
                foreach (var line in text.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        _logger.Write(_level, "{Direction}: {Message}", direction, trimmed);
                }
            }

            public void Dispose() { }
        }

        /// <summary>
        /// 建立 MimeMessage (支援多收件人/CC/BCC，使用逗號或分號分隔)
        /// </summary>
        private MimeMessage CreateEmailMessage(MailRequest mailRequest)
        {
            var email = new MimeMessage();

            // From / Sender
            var fromAddress = string.IsNullOrWhiteSpace(mailRequest.From) ? _mailSettings.Mail : mailRequest.From.Trim();
            email.Sender = MailboxAddress.Parse(fromAddress);
            email.From.Add(MailboxAddress.Parse(fromAddress));

            // To
            foreach (var addr in SplitAddresses(mailRequest.ToEmail))
            {
                email.To.Add(MailboxAddress.Parse(addr));
            }

            // CC
            if (!string.IsNullOrWhiteSpace(mailRequest.Cc))
            {
                foreach (var addr in SplitAddresses(mailRequest.Cc))
                {
                    email.Cc.Add(MailboxAddress.Parse(addr));
                }
            }

            // BCC
            if (!string.IsNullOrWhiteSpace(mailRequest.Bcc))
            {
                foreach (var addr in SplitAddresses(mailRequest.Bcc))
                {
                    email.Bcc.Add(MailboxAddress.Parse(addr));
                }
            }

#if DEBUG
            // 在 DEBUG 編譯時，強制覆蓋收件人（避免意外發送到正式人員）
            LogDebug("DEBUG 組態下覆寫收件人 -> markchu929@gmail.com");
            email.To.Clear();
            email.To.Add(MailboxAddress.Parse("markchu929@gmail.com"));
#endif

            email.Subject = string.IsNullOrEmpty(mailRequest.Subject)
                ? $"[{_mailSettings.DisplayName}] {DateTime.Now:yyyy/MM/dd HH:mm}"
                : mailRequest.Subject;

            var builder = new BodyBuilder();
            if (mailRequest.IsHtml == true)
            {
                builder.HtmlBody = mailRequest.Body ?? string.Empty;
            }
            else
            {
                builder.TextBody = mailRequest.Body ?? string.Empty;
            }

            // Attachments
            if (mailRequest.Attachments != null && mailRequest.Attachments.Count > 0)
            {
                foreach (var file in mailRequest.Attachments.Where(f => f != null && f.Length > 0))
                {
                    using var ms = new MemoryStream();
                    file.CopyTo(ms);
                    builder.Attachments.Add(file.FileName, ms.ToArray(), ContentType.Parse(file.ContentType));
                    LogDebug($"加入附件: {file.FileName} ({file.Length} bytes)");
                }
            }

            email.Body = builder.ToMessageBody();

            LogDebug($"郵件建立完成 -> To: {string.Join(';', email.To.Select(a => a.ToString()))}, Subject: {email.Subject}");
            return email;
        }

        private SecureSocketOptions GetSecureSocketOptions(int sslOption) => sslOption switch
        {
            0 => SecureSocketOptions.None,
            1 => SecureSocketOptions.Auto,
            2 => SecureSocketOptions.SslOnConnect,
            3 => SecureSocketOptions.StartTls,
            4 => SecureSocketOptions.StartTlsWhenAvailable,
            _ => SecureSocketOptions.None,
        };

        private async Task SendEmailAsync(SmtpClient smtp, MimeMessage email, SecureSocketOptions sslOption, bool isAuth)
        {
            try
            {
                LogDebug("連線到 SMTP 伺服器...");
                await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, sslOption);
                LogDebug("已連線");

                if (isAuth)
                {
                    if (string.IsNullOrWhiteSpace(_mailSettings.Password))
                        throw new InvalidOperationException("MailSettings.Password 不可為空，因為 IsAuth 已啟用。");

                    LogDebug("進行身份驗證...");
                    await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);
                    LogDebug("身份驗證成功");
                }

                LogDebug("傳送郵件中...");
                await smtp.SendAsync(email);
                LogDebug("郵件已送出，斷線中...");
                await smtp.DisconnectAsync(true);
                LogDebug("SMTP 連線關閉");
            }
            catch (Exception)
            {
                throw; // 保持往上層處理
            }
        }

        private static IEnumerable<string> SplitAddresses(string addresses)
        {
            return addresses
                .Split(new[] { ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a));
        }

        private string BuildErrorMessage(Exception ex)
        {
            var parts = new List<string> { ex.GetType().Name, ex.Message };
            if (ex.InnerException != null)
            {
                parts.Add($"Inner: {ex.InnerException.GetType().Name} {ex.InnerException.Message}");
            }
            return string.Join(" | ", parts);
        }

        private void LogDebug(string message)
        {
            if (!_mailSettings.Debug) return;
            _logger.Debug("{Message}", message);
        }

        private void LogError(string message, Exception ex)
        {
            if (!_mailSettings.Debug)
            {
                // 即使 Debug=false 仍要記錄錯誤於 Error 層級給集中式日誌
                _logger.Error(ex, "{Message}", message);
            }
            else
            {
                _logger.Error(ex, "{Message}", message);
            }
        }
    }
}
