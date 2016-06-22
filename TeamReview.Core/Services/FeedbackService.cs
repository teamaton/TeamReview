using System.Linq;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;
using TeamReview.Core.ViewModels;

namespace TeamReview.Core.Services {
	public class FeedbackService : IFeedbackService {
		private readonly IDatabaseContext _databaseContext;

		public FeedbackService(IDatabaseContext databaseContext) {
			_databaseContext = databaseContext;
		}

		public void SaveFeedback(string userEmail, FeedbackViewModel feedback) {
			var reviewFeedback =
				new ReviewFeedback
					{
						Reviewer = _databaseContext.UserProfiles.FirstOrDefault(user => user.EmailAddress == userEmail)
					};

			var reviewconfiguration = _databaseContext.ReviewConfigurations.Find(feedback.ReviewId);

			foreach (var categoriesWithPeersAndRating in feedback.CategoriesWithPeersAndRatings) {
				foreach (var peersWithRating in categoriesWithPeersAndRating.PeersWithRatings) {
					reviewFeedback.Assessments.Add(
						new Assessment
							{
								Rating = peersWithRating.Rating,
								Reviewer = reviewFeedback.Reviewer,
								ReviewCategory = _databaseContext.ReviewCategories.Find(categoriesWithPeersAndRating.Category.Id),
								ReviewedPeer = _databaseContext.UserProfiles.Find(peersWithRating.Peer.Id)
							});
				}
			}

			reviewconfiguration.GetCurrentFeedback().Add(reviewFeedback);
			_databaseContext.SaveChanges();
		}
	}
}