using EzLib.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EzLib.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
        public MailService(MailSettings mailSettings)
        {
            _mailSettings = mailSettings;
        }

        public async Task<MailResult> SendEmailAsync(MailRequest mailRequest)
        {
            var result = new MailResult();

            try
            {
                var email = CreateEmailMessage(mailRequest);
                var sslOption = GetSecureSocketOptions(_mailSettings.SSL);
                var IsAuth = _mailSettings.IsAuth;

                if (_mailSettings.Debug)
                {
                    using var smtp = new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
                    smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtp.CheckCertificateRevocation = false;
                    await SendEmailAsync(smtp, email, sslOption, IsAuth);
                }
                else
                {
                    using var smtp = new SmtpClient();
                    await SendEmailAsync(smtp, email, sslOption, IsAuth);
                }

                result.IsSuccess = true;
                result.Message = "Email sent successfully.";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
            }

            return result;
        }
        /// <summary>
        /// 創建 MimeMessage 物件，並設置發件人、收件人、主題和內容。
        /// </summary>
        /// <param name="mailRequest"></param>
        /// <returns></returns>
        private MimeMessage CreateEmailMessage(MailRequest mailRequest)
        {
            var email = new MimeMessage();
            // 使用 req.From 或 _mailSettings.Mail
            email.Sender = MailboxAddress.Parse(string.IsNullOrEmpty(mailRequest.From) ? _mailSettings.Mail : mailRequest.From);

#if DEBUG
            email.To.Add(MailboxAddress.Parse("markchu929@gmail.com"));
#else
            email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
#endif

            email.Subject = string.IsNullOrEmpty(mailRequest.Subject)
                ? $"[{_mailSettings.DisplayName}] {DateTime.Now:yyyy/MM/dd HH:mm}"
                : mailRequest.Subject;

            var builder = new BodyBuilder();
            if (mailRequest.IsHtml == true)
            {
                builder.HtmlBody = mailRequest.Body;
            }
            else 
            { 
                builder.TextBody = mailRequest.Body; 
            }

            if (mailRequest.Attachments != null)
            {
                foreach (var file in mailRequest.Attachments)
                {
                    if (file.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        file.CopyTo(ms);
                        builder.Attachments.Add(file.FileName, ms.ToArray(), ContentType.Parse(file.ContentType));
                    }
                }
            }

            email.Body = builder.ToMessageBody();
            return email;
        }


        private SecureSocketOptions GetSecureSocketOptions(int sslOption)
        {
            return sslOption switch
            {
                0 => SecureSocketOptions.None,
                1 => SecureSocketOptions.Auto,
                2 => SecureSocketOptions.SslOnConnect,
                3 => SecureSocketOptions.StartTls,
                4 => SecureSocketOptions.StartTlsWhenAvailable,
                _ => SecureSocketOptions.None,
            };
        }

        private async Task SendEmailAsync(SmtpClient smtp, MimeMessage email, SecureSocketOptions sslOption, bool IsAuth)
        {
            try
            {
                await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, sslOption);
                if (IsAuth) 
                {
                    await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);
                }
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                // Trace.WriteLine("Email sent successfully.");
            }
            catch (Exception)
            {
                // Trace.WriteLine($"Failed to send email: {ex.Message}");
                throw; // 重新拋出異常以便上層處理
            }
        }
    }
}
