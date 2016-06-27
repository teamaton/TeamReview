using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace TeamReview.Core.Services
{
    public class LiteSmtpClient : ISmtpClient
    {
        public SmtpClient Create()
        {
            var user = ConfigurationManager.AppSettings["SMTPUser"];
            var password = ConfigurationManager.AppSettings["SMTPPassword"];
            var host = ConfigurationManager.AppSettings["SMTPServer"];
            var port = ConfigurationManager.AppSettings["SMTPPort"];

            return new SmtpClient(host, int.Parse(port)) {Credentials = new NetworkCredential(user, password)};
        }
    }
}