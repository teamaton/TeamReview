using System;
using System.Collections.Generic;
using System.Linq;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;

namespace TeamReview.Core.Services {
	public class ReviewService : IReviewService {
		private readonly IDatabaseContext _databaseContext;
		private readonly IEmailService _emailService;

		public ReviewService(IDatabaseContext databaseContext, IEmailService emailService) {
			_databaseContext = databaseContext;
			_emailService = emailService;
		}

		public void StartReview(int reviewConfigurationId, string email) {
			var reviewConfiguration = _databaseContext.ReviewConfigurations.Single(c => c.Id == reviewConfigurationId);
			reviewConfiguration.Active = true;
			if (reviewConfiguration.Initiator == null) {
				reviewConfiguration.Initiator = _databaseContext.UserProfiles.Single(user => user.EmailAddress == email);
			}
			AddAndStartNewRound(reviewConfiguration);
		}

		public void StartNewReviewRounds() {
			var reviewConfigurationsReadyForNextRound = GetReviewConfigurationsWhereANewReviewRoundShouldBeStarted();
			foreach (var reviewConfiguration in reviewConfigurationsReadyForNextRound) {
				AddAndStartNewRound(reviewConfiguration);
			}
		}

		private IEnumerable<ReviewConfiguration> GetReviewConfigurationsWhereANewReviewRoundShouldBeStarted() {
			var reviewConfigurations = _databaseContext.ReviewConfigurations
				.ToList();
			return reviewConfigurations.Where(review =>
					review.ReviewRounds.Any(
						round =>
						round.Active &&
						round.StartDate <= DateTime.UtcNow.Date.AddDays(-7 * review.LengthOfReviewRoundInWeeks)));
		}

		private void AddAndStartNewRound(ReviewConfiguration reviewConfiguration) {
			var activeRound = reviewConfiguration.GetCurrentReviewRound();
			if (activeRound != null) {
				activeRound.Active = false;
			}

			reviewConfiguration.ReviewRounds.Add(new ReviewRound
				                                     {
					                                     Active = true,
					                                     StartDate = activeRound != null
						                                                 ? activeRound.StartDate.AddDays(
							                                                 reviewConfiguration.LengthOfReviewRoundInWeeks*7).Date
						                                                 : DateTime.UtcNow.Date
				                                     });

			_databaseContext.SaveChanges();
			//_emailService.SendInvitationEmailsForReview(reviewConfiguration.Id);
		}
	}
}