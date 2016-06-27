using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;
using AutoMapper;
using TeamReview.Core;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;
using TeamReview.Core.Services;
using TeamReview.Core.ViewModels;
using TeamReview.Web.Filters;

namespace TeamReview.Web.Controllers {
	[Authorize, AccessControl]
	public class FeedbackController : Controller {
		private readonly IDatabaseContext _db;
		private readonly IFeedbackService _feedbackService;
	    private readonly ISmtpClient _smtpClient;

	    public FeedbackController(IDatabaseContext dbContext, IFeedbackService feedbackService, ISmtpClient smtpClient) {
			_db = dbContext;
			_feedbackService = feedbackService;
	        _smtpClient = smtpClient;
	    }

		[HttpGet]
		[DenyDuplicateFeedback]
		public ActionResult Provide(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			var feedback = new FeedbackViewModel { ReviewId = id, ReviewName = reviewconfiguration.Name };

			foreach (var reviewCategory in reviewconfiguration.Categories) {
				var categoryWithPeersAndRatings = new CategoryWithPeersAndRatings();
				categoryWithPeersAndRatings.Category = Mapper.Map<CategoryShowModel>(reviewCategory);
				foreach (var peer in reviewconfiguration.Peers) {
					var peerWithRating = new PeerWithRating { Peer = Mapper.Map<PeerShowModel>(peer), Rating = -1 };
					categoryWithPeersAndRatings.PeersWithRatings.Add(peerWithRating);
				}
				feedback.CategoriesWithPeersAndRatings.Add(categoryWithPeersAndRatings);
			}
			return View(feedback);
		}

		[HttpPost]
		[DenyDuplicateFeedback]
		public ActionResult Provide(int id, FeedbackViewModel feedback) {
			// Check for request integrity
			if (id != feedback.ReviewId) {
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Not allowed.");
			}

			if (feedback.IsIncomplete) {
				TempData["Message"] = "Please fill out all ratings.";
				return View(feedback);
			}

			_feedbackService.SaveFeedback(User.Identity.Name, feedback);

			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			SendMailToPeersIfAllHaveProvidedFeedback(reviewconfiguration);

			TempData["Message"] = "Review has been completed.";
			return RedirectToAction("Index", "Review");
		}

		private void SendMailToPeersIfAllHaveProvidedFeedback(ReviewConfiguration review) {
			//_db.Entry(review).Collection(c => c.Peers).Load();
			//_db.Entry(review).Collection(c => c.Feedback).Load();

			if (review.Peers.Any(peer => review.GetCurrentFeedback().Select(f => f.Reviewer).All(r => r.Id != peer.Id))) {
				return;
			}

			foreach (var peer in review.Peers) {
				var message = new MailMessage(EmailService.DefaultContactEmail, peer.EmailAddress)
					              {
						              Subject = "Review Complete",
						              Body = GetMailBodyForFinishedReview(peer.UserName, review.Id, review.Name)
					              };

				_smtpClient.Create().Send(message);
			}
		}

		private static string GetMailBodyForFinishedReview(string userName, int reviewId, string reviewName) {
			return string.Format(
				@"Hi there, {0},

All peers have provided their feedback for review '{1}'.

Please visit the following link to view the results of the review:

http://www.teamreview.net/Review/Results/{2}

If you would like to find out more about TeamReview, feel free to visit http://www.teamreview.net/.

In case you have any questions, just reply to this email and we will get in touch with you as soon as possible.


Thank you for your time and cheers,

Andrej - Masterchief Head of Design of TeamReview.net
",
				userName, reviewName, reviewId);
		}
	}
}