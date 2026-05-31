using EzLib.Models;
using System.Threading.Tasks;
using EzLib.Services;
using Serilog;
namespace EzLib;

public class SmtpMailer
{
    private readonly IMailService mailservice;
    public SmtpMailer(MailSettings settings, ILogger? logger = null)
    {
        // Set up mail service
        this.mailservice = new MailService(settings, logger);
    }
    public async Task<MailResult> SendAsync(MailRequest request)
    {
        // Send email
        return await mailservice.SendEmailAsync(request);
    }
}
