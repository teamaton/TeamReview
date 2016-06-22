using System.Collections.Generic;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Services;

namespace TeamReview.SpecsWithoutBrowser.StepDefinitions.Models {
	public class FakeEmailService : EmailService {
		public IList<string> Receivers { get; set; }
		public int NumberOfSentEmails;

		public FakeEmailService(IDatabaseContext databaseContext) : base(databaseContext) {
			Receivers = new List<string>();
		}

		protected override void Send(string sender, string receiver, string subject, string body) {
			NumberOfSentEmails++;
			Receivers.Add(receiver);
		}
	}
}