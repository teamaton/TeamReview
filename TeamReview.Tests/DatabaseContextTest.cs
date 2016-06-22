using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TeamReview.Core.Models;

namespace TeamReview.Tests {
	[TestFixture]
	public class DatabaseContextTest : DatabaseEnabledTestBase {
		[Test]
		public void Should_save_review_with_two_feedback_rounds() {
			var configuration = new ReviewConfiguration
				                    {
					                    Name = "test config",
										LengthOfReviewRoundInWeeks = 1,
										ReviewRounds = new List<ReviewRound>
						                                   {
							                                   new ReviewRound { StartDate = DateTime.UtcNow.AddDays(-1), Active = true },
							                                   new ReviewRound { StartDate = DateTime.UtcNow.AddDays(30), Active = false },
						                                   }
				                    };

			DatabaseContext.ReviewConfigurations.Add(configuration);
			DatabaseContext.SaveChanges();
			CreateNewContext();

			Assert.AreEqual(2, DatabaseContext.ReviewConfigurations.First().ReviewRounds.Count());
		}
	}
}