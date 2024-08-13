using EzLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzLib.Services
{
    public interface IMailService
    {
        Task<MailResult> SendEmailAsync(MailRequest mailRequest);
    }
}
