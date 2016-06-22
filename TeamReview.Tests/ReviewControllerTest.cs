using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using TeamReview.Core.Services;
using TeamReview.Web.Controllers;

namespace TeamReview.Tests {
	[TestFixture]
	public class ReviewControllerTest {
		[Test]
		public void Should_check_reviews_for_new_rounds() {
			var mock = new Mock<IReviewService>();
			mock.Setup(m => m.StartNewReviewRounds()).Verifiable();
			var reviewController = CreateReviewControllerForUser("test", mock.Object);

			reviewController.CheckForNewReviewRounds();

			mock.Verify(m => m.StartNewReviewRounds());
		}

		private ReviewController CreateReviewControllerForUser(string userName, IReviewService reviewService) {
			var mockedControllerContext = new Mock<ControllerContext>();
			mockedControllerContext.SetupGet(p => p.HttpContext.User.Identity.Name).Returns(userName);
			mockedControllerContext.SetupGet(p => p.HttpContext.Request.IsAuthenticated).Returns(true);

			return new ReviewController(null, reviewService) { ControllerContext = mockedControllerContext.Object };
		}
	}
}