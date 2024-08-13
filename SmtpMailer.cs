using EzLib.Models;
using System.Threading.Tasks;
using EzLib.Services;
namespace EzLib;

public class SmtpMailer
{
    private readonly IMailService mailservice;
    public SmtpMailer(MailSettings settings)
    {
        // Set up mail service
        this.mailservice = new MailService(settings);
    }
    public async Task<MailResult> SendAsync(MailRequest request)
    {
        // Send email
        return await mailservice.SendEmailAsync(request);
    }
}
