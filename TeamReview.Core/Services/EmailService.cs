using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;

namespace TeamReview.Core.Services {
	public class EmailService : IEmailService {
		private readonly IDatabaseContext _databaseContext;

        // todo: move to web.config and use the email of your admin
        // default email as sender of automatic emails and for receiving automatic messages
        public static string DefaultContactEmail = "admin@my-domain.com";

		public EmailService(IDatabaseContext databaseContext) {
			_databaseContext = databaseContext;
		}

		public void SendInvitationEmailsForReview(int reviewConfigurationId) {
			var configuration = _databaseContext.ReviewConfigurations.Single(review => review.Id == reviewConfigurationId);
			SendMailToPeers(configuration.Peers, configuration.Id, configuration.Initiator);
		}

		private void SendMailToPeers(IEnumerable<UserProfile> peers, int reviewId, UserProfile owner) {
			foreach (var peer in peers) {
				const string subject = "Provide Review";
				var sender = DefaultContactEmail;
				var receiver = peer.EmailAddress;
				var body = GetMailBodyForStartedReview(peer.UserName, reviewId, owner);
				Send(sender, receiver, subject, body);
			}
		}

		private static string GetMailBodyForStartedReview(string userName, int reviewId, UserProfile owner) {
			return String.Format(
				@"Hi there, {0},

you have been invited by {2} ({3}) to provide a review.

This helps improve your team's and your own performance.                

Please visit the following link to provide the review:

http://www.teamreview.net/Feedback/Provide/{1}

If you would like to find out more about TeamReview, feel free to visit http://www.teamreview.net/.

In case you have any questions, just reply to this email and we will get in touch with you as soon as possible.


Thank you for your time and cheers,

Andrej - Masterchief Head of Design of TeamReview.net
",
				userName, reviewId, owner.UserName, owner.EmailAddress);
		}

		protected virtual void Send(string sender, string receiver, string subject, string body) {
			var smtpClient = new SmtpClient();
			var message = new MailMessage(sender, receiver)
				              {
					              Subject = subject,
					              Body = body
				              };
			smtpClient.Send(message);
		}
	}
}