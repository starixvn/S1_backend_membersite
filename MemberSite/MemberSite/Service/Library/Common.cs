using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

using MemberSite.Service.Model;

using Microsoft.Extensions.Options;

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
            if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "Local")
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
            return "P";
        }

        public async Task SendMailAsync(string FromName, string FromAddress, string ToName, string ToAddress, string Subject, string Content)
        {
            var _mailConfig = mailConfig.Value;
            var client = new SmtpClient
            {
                Host = _mailConfig.MailHost,
                //Credentials = new NetworkCredential(_mailConfig.MailID, _mailConfig.MailPwd),
                Port = _mailConfig.Port,
                //EnableSsl = true,
                //UseDefaultCredentials = false
            };

            var mailMessage = new MailMessage();
            mailMessage.IsBodyHtml = true;
            mailMessage.From = new MailAddress(_mailConfig.MailID, FromName);
            mailMessage.To.Add(new MailAddress(ToAddress, ToName));
            mailMessage.Body = Content;
            mailMessage.Subject = Subject;
            await client.SendMailAsync(mailMessage);
        }

        public string ToMD5(string encodeStr)
        {
            using (var cryptoMD5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(encodeStr);
                var hash = cryptoMD5.ComputeHash(bytes);
                var md5 = BitConverter.ToString(hash)
                  .Replace("-", String.Empty);

                return md5;
            }
        }
    }
}
