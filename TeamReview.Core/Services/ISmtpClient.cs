using System.Net.Mail;

namespace TeamReview.Core.Services
{
    public interface ISmtpClient
    {
        SmtpClient Create();
    }
}