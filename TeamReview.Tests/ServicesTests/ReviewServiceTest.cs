using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using TeamReview.Core.Models;
using TeamReview.Core.Services;

namespace TeamReview.Tests.ServicesTests {
	[TestFixture]
	public class ReviewServiceTest : DatabaseEnabledTestBase {
		[Test]
		public void Start_new_round_when_start_date_is_one_week_ago() {
			// Assign
			var mockedEmailService = new Mock<IEmailService>();
			const int lengthOfReviewRoundInWeeks = 1;
			var configuration = new ReviewConfiguration
				                    {
					                    Name = "test config",
										LengthOfReviewRoundInWeeks = lengthOfReviewRoundInWeeks,
					                    ReviewRounds = new List<ReviewRound>
						                                   {
							                                   new ReviewRound { StartDate = DateTime.UtcNow.Date.AddDays(-(lengthOfReviewRoundInWeeks * 7)), Active = true }
						                                   }
				                    };
			DatabaseContext.ReviewConfigurations.Add(configuration);
			DatabaseContext.SaveChanges();
			IReviewService reviewService = new ReviewService(DatabaseContext, mockedEmailService.Object);

			// Act
			reviewService.StartNewReviewRounds();

			// Assert
			Assert.AreEqual(2, DatabaseContext.ReviewConfigurations.First().ReviewRounds.Count());
			var currentReviewRound = DatabaseContext.ReviewConfigurations.First().GetCurrentReviewRound();
			Assert.IsNotNull(currentReviewRound);
			Assert.GreaterOrEqual(currentReviewRound.StartDate, DateTime.UtcNow.Date.AddMinutes(-1));
			Assert.LessOrEqual(currentReviewRound.StartDate, DateTime.UtcNow.Date.AddMinutes(1));
		}

		[Test]
		public void Do_not_start_new_round_when_start_date_less_than_one_week_ago() {
			// Assign
			var mockedEmailService = new Mock<IEmailService>();
			const int lengthOfReviewRoundInWeeks = 1;
			const int reviewLengthMinusOneDay = lengthOfReviewRoundInWeeks * 7 - 1;
			var configuration = new ReviewConfiguration
				                    {
					                    Name = "test config",
										LengthOfReviewRoundInWeeks = lengthOfReviewRoundInWeeks,
										ReviewRounds = new List<ReviewRound>
						                                   {
							                                   new ReviewRound { StartDate = DateTime.UtcNow.AddDays(-reviewLengthMinusOneDay), Active = true }
						                                   }
				                    };
			DatabaseContext.ReviewConfigurations.Add(configuration);
			DatabaseContext.SaveChanges();
			IReviewService reviewService = new ReviewService(DatabaseContext, mockedEmailService.Object);

			// Act
			reviewService.StartNewReviewRounds();

			// Assert
			Assert.AreEqual(1, DatabaseContext.ReviewConfigurations.First().ReviewRounds.Count());
			var currentReviewRound = DatabaseContext.ReviewConfigurations.First().GetCurrentReviewRound();
			Assert.IsNotNull(currentReviewRound);
			Assert.LessOrEqual(currentReviewRound.StartDate, DateTime.UtcNow.AddDays(-5));
		}
	}
}