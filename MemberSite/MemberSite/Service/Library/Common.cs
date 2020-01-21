using MemberSite.Service.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MemberSite.Service.Library
{
    public class Common
    {
        private readonly IWebHostEnvironment environment;
        private readonly IOptions<MailConfig> mailConfig;

        public Common(IWebHostEnvironment environment, IOptions<MailConfig> mailConfig)
        {
            this.environment = environment;
            this.mailConfig = mailConfig;
        }
        public string CreateSerialId()
        {
            return $"{GetEnv()}{DateTime.Now.ToString("yyMMddhhmmss")}{new Random(Guid.NewGuid().GetHashCode()).Next(10000, 99999)}";
        }
        public string GetEnv()
        {
            if (environment.EnvironmentName == "Development")
            {
                return "D";
            }
            else if (environment.EnvironmentName == "Staging")
            {
                return "S";
            }
            else if (environment.EnvironmentName == "Production")
            {
                return "P";
            }
            else if (environment.EnvironmentName == "QA")
            {
                return "Q";
            }
            return "X";
        }

        public async Task SendMailAsync(string FromName, string FromAddress, string ToName, string ToAddress, string Subject, string Content)
        {
            var _mailConfig = mailConfig.Value;
            var client = new SmtpClient
            {
                Host = _mailConfig.MailHost,
                Credentials = new NetworkCredential(_mailConfig.MailID, _mailConfig.MailPwd),
                Port = _mailConfig.Port,
                EnableSsl = true,
                UseDefaultCredentials = false
            };

            MailMessage mailMessage = new MailMessage();
            mailMessage.IsBodyHtml = true;
            mailMessage.From = new MailAddress(_mailConfig.MailID, FromName);
            mailMessage.To.Add(new MailAddress(ToAddress, ToName));
            mailMessage.Body = Content;
            mailMessage.Subject = Subject;
            await client.SendMailAsync(mailMessage);
        }
    }
}
