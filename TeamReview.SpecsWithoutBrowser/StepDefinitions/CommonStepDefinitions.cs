using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using TeamReview.Core;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;
using TeamReview.Core.Services;
using TeamReview.Core.ViewModels;
using TeamReview.SpecsWithoutBrowser.StepDefinitions.Models;
using TeamReview.Web.Controllers;
using TeamReview.Web.ViewModels;
using TechTalk.SpecFlow;

namespace TeamReview.SpecsWithoutBrowser.StepDefinitions {
	[Binding]
	public class CommonStepDefinitions : StepsBase {
		// the initial user must match one of the peers in the scenarios
		private readonly string _defaultUserEmailAddress = EmailService.DefaultContactEmail;
		private const string DefaultUserName =  "Test";

		private FakeEmailService _fakeEmailService;
	    private readonly string _emailDomainEnding = "@my-domain.com";

	    public CommonStepDefinitions(ReviewInfo reviewInfo, CommonContext contextContext)
			: base(reviewInfo, contextContext) {
		}

		[BeforeScenario]
		public void BeforeScenario() {
			// "You should avoid using DI in unit testing" - from: http://stackoverflow.com/a/11093830/177710
			// That's why there's no Autofac here.
			_context.DatabaseContext = new DatabaseContext(CreateNewDatabase());
			_fakeEmailService = new FakeEmailService(_context.DatabaseContext);
			AutoMapperBootstrap.Initialize();
		}

		[BeforeStep]
		public void BeforeStep() {
			// construct the controllers at the beginning of each step
			_context.ReviewController = new Lazy<ReviewController>(CreateReviewControllerForCurrentUser);
			_context.FeedbackController = new Lazy<FeedbackController>(CreateFeedbackControllerForCurrentUser);
			_context.ReportController = new Lazy<ReportController>(CreateReportControllerForCurrentUser);
		}

		[AfterStep]
		public void AfterStep() {
			_context.DatabaseContext.SaveChanges();
		}

		// ReSharper disable once InconsistentNaming
		[Given(@"I am a registered user and logged in")]
		public void GivenIAmARegisteredUser() {
			var user = new UserProfile {EmailAddress = _defaultUserEmailAddress, UserName = DefaultUserName};
			_context.DatabaseContext.UserProfiles.Add(user);
			_reviewInfo.CurrentUser = user;
		}

		[When(@"I create a new review")]
		public void WhenICreateANewReview(Table table) {
			var reviewCreateViewModel =
				new ReviewCreateEditModel
					{
						Name = table.Rows.First()["Name"],
						LengthOfReviewRoundInWeeks = Convert.ToInt32(table.Rows.First()["Length in weeks"]),
						AddedCategories = new List<CategoryAddModel>
							                  {
								                  new CategoryAddModel
									                  {
										                  Name = table.Rows.First()["Category 1 Name"],
										                  Description = table.Rows.First()["Category 1 description"]
									                  },
								                  new CategoryAddModel
									                  {
										                  Name = table.Rows.First()["Category 2 Name"],
										                  Description = table.Rows.First()["Category 2 description"]
									                  }
							                  }
					};
			ActionResult result = _context.ReviewController.Value.Create(reviewCreateViewModel, "Save");

			ScenarioContext.Current.Set(table);
			ScenarioContext.Current.Set(result);
			ScenarioContext.Current.Set(_context.ReviewController.Value.TempData);
		}

		[Given]
		public void GivenIHaveAReviewWithPeers_P0_And_P1(string p0, string p1) {
			GivenIAmARegisteredUser();
			var loggedInUser = _reviewInfo.CurrentUser;
			var review = _context.DatabaseContext.ReviewConfigurations.Add(
				new ReviewConfiguration
					{
						Name = "Rev1",
						LengthOfReviewRoundInWeeks = 1,
						Categories = {new ReviewCategory {Name = "First", Description = "Description of first category"}},
						Peers =
							{
								loggedInUser,
								new UserProfile {UserName = p0, EmailAddress = p0 + _emailDomainEnding},
								new UserProfile {UserName = p1, EmailAddress = p1 + _emailDomainEnding},
							}
					});
			_context.DatabaseContext.SaveChanges();
			// EF cannot map the double reference on the same entity via the list of peers 
			// and the initiator property automatically - other solutions?
			review.Initiator = review.Peers.First();
			_reviewInfo.ReviewConfiguration = review;
			ScenarioContext.Current.Set(review.Peers);
		}

		[Given(@"I have started the review one week ago")]
		public void GivenIHaveStartedTheReviewOneWeekAgo() {
			var review = _context.DatabaseContext.ReviewConfigurations.First();
			review.ReviewRounds.Add(new ReviewRound
				                        {
					                        Active = true,
					                        StartDate =
						                        DateTime.UtcNow.Date.AddDays(-7*review.LengthOfReviewRoundInWeeks)
				                        });
		}

		[When(@"the program checks for new rounds")]
		public void WhenTheProgramChecksForNewRounds() {
			_context.ReviewController.Value.CheckForNewReviewRounds();
		}

		[Then(@"a new round is started")]
		public void ThenANewRoundIsStarted() {
			var review = _context.DatabaseContext.ReviewConfigurations.First();
			Assert.AreEqual(2, review.ReviewRounds.Count);

			var currentReviewRound = review.GetCurrentReviewRound();
			Assert.GreaterOrEqual(currentReviewRound.StartDate, DateTime.UtcNow.Date.AddMinutes(-1));
			Assert.LessOrEqual(currentReviewRound.StartDate, DateTime.UtcNow.Date.AddMinutes(1));
		}

		[Then(@"I can provide a second feedback of review")]
		public void ThenICanProvideASecondFeedbackOfReview() {
			var review = _context.DatabaseContext.ReviewConfigurations.First();
			var model = ((ViewResultBase) _context.FeedbackController.Value.Provide(review.Id)).Model as FeedbackViewModel;

			Assert.NotNull(model, "The view model cannot be empty.");
			Assert.AreEqual(model.CategoriesWithPeersAndRatings.Count, review.Categories.Count,
			                "The number of review categories do not match.");
			Assert.AreEqual(model.CategoriesWithPeersAndRatings.First().Category.Name,
			                review.Categories.First().Name,
			                "The names of review categories do not match.");

			var peerAddresses = model.CategoriesWithPeersAndRatings.SelectMany(c => c.PeersWithRatings)
				.Select(p => p.Peer.EmailAddress).ToList();
			var expectedEmailAddresses =
				ScenarioContext.Current.Get<IList<UserProfile>>().Select(user => user.EmailAddress);
			CollectionAssert.AreEquivalent(expectedEmailAddresses, peerAddresses, "The peers do not match.");

			ScenarioContext.Current.Set(model);
		}

		[When(@"I provide data for the second round of review")]
		public void WhenIProvideDataForTheSecondRoundOfReview() {
			var review = _context.DatabaseContext.ReviewConfigurations.First();
			var model = ScenarioContext.Current.Get<FeedbackViewModel>();

			foreach (CategoryWithPeersAndRatings combinedCategory in model.CategoriesWithPeersAndRatings) {
				foreach (PeerWithRating peer in combinedCategory.PeersWithRatings) {
					peer.Rating = GetRatingFromId(peer.Peer.Id);
				}
			}

			_context.FeedbackController.Value.Provide(review.Id, model);
		}

		[Then(@"my feedback is saved")]
		public void ThenMyFeedbackIsSaved() {
			var assessments = _context.DatabaseContext.Assessments.ToList();
			Assert.AreEqual(3, assessments.Count);
			foreach (Assessment assessment in assessments) {
				Assert.AreEqual(assessment.Rating, GetRatingFromId(assessment.ReviewedPeer.Id));
			}
		}

		private static int GetRatingFromId(int id) {
			return id%2 + 1;
		}

		[Then(@"Kai and Anton receive an email with an invitation link to the new round of review")]
		public void ThenKaiAndAntonReceiveAnEmailWithAnInvitationLinkToTheNewRoundOfReview() {
			var peers = ScenarioContext.Current.Get<IList<UserProfile>>();
			Assert.GreaterOrEqual(_fakeEmailService.NumberOfSentEmails, 2);
			foreach (UserProfile peer in peers) {
				Assert.IsTrue(_fakeEmailService.Receivers.Contains(peer.EmailAddress));
			}
		}

		[Then(@"my new review was created with those categories")]
		public void ThenMyNewReviewWasCreatedWithThoseCategories() {
			var review = _context.DatabaseContext.ReviewConfigurations.First();
			var reviewTable = ScenarioContext.Current.Get<Table>();

			Assert.AreEqual(1, _context.DatabaseContext.ReviewConfigurations.Count());
			Assert.IsNotNull(reviewTable);
			Assert.AreEqual(reviewTable.Rows.First()["Name"], review.Name);
			Assert.AreEqual(reviewTable.Rows.First()["Length in weeks"], review.LengthOfReviewRoundInWeeks.ToString());
			Assert.AreEqual(reviewTable.Rows.First()["Category 1 Name"], review.Categories.First().Name);
			Assert.AreEqual(reviewTable.Rows.First()["Category 1 description"], review.Categories.First().Description);
			Assert.AreEqual(reviewTable.Rows.First()["Category 2 Name"], review.Categories.Last().Name);
			Assert.AreEqual(reviewTable.Rows.First()["Category 2 description"], review.Categories.Last().Description);
		}

		[Then(@"I am added to the review")]
		public void ThenIAmAddedToTheReview() {
			var review = _context.DatabaseContext.ReviewConfigurations.First();
			Assert.AreEqual(_reviewInfo.CurrentUser.EmailAddress, review.Peers.First().EmailAddress);
		}

		[Then(@"I am on the Dashboard")]
		public void ThenIAmOnTheDashboard() {
			var result = ScenarioContext.Current.Get<ActionResult>() as RedirectToRouteResult;
			Assert.IsNotNull(result);
			Assert.AreEqual("Index", result.RouteValues["action"]);
		}

		[Then(@"I see the message ""(.*)""")]
		public void ThenISeeTheMessage(string message) {
			var tempDataDictionary = ScenarioContext.Current.Get<TempDataDictionary>();
			Assert.IsNotNull(tempDataDictionary);
			Assert.AreEqual(message, tempDataDictionary["Message"]);
		}

		[Given]
		public void GivenIHaveAReviewWithCategories_P0_And_P1(string p0, string p1) {
			GivenIAmARegisteredUser();
			var review = _context.DatabaseContext.ReviewConfigurations.Add(
				new ReviewConfiguration
					{
						Name = "Rev1",
						LengthOfReviewRoundInWeeks = 1,
						Categories = {new ReviewCategory {Name = p0}, new ReviewCategory {Name = p1}}
					});
			_context.DatabaseContext.SaveChanges();
			_reviewInfo.ReviewConfiguration = review;
		}

		[When]
		public void WhenIDeleteCategory_P0(string p0) {
			var review = _reviewInfo.ReviewConfiguration;
			var categoryToDelete = review.Categories.Single(cat => cat.Name == p0);
			var jsonResult = _context.ReviewController.Value.RemoveCategory(
				new RemoveCategoryViewModel {ReviewId = review.Id, CategoryId = categoryToDelete.Id}, review.Id);
			Assert.AreEqual("success", ((JsonResultDataModel) jsonResult.Data).status);
		}

		[Then]
		public void ThenTheReviewHasOnlyCategory_P0(string p0) {
			var categories = _context.DatabaseContext.ReviewConfigurations.Single().Categories;
			Assert.AreEqual(1, categories.Count);
			Assert.AreEqual(p0, categories[0].Name);
		}

		[When]
		public void WhenIDeletePeer_P0(string p0) {
			var review = _reviewInfo.ReviewConfiguration;
			var peerToDelete = review.Peers.Single(peer => peer.UserName == p0);
			var jsonResult = _context.ReviewController.Value.RemovePeer(
				new RemovePeerViewModel {ReviewId = review.Id, PeerEmail = peerToDelete.EmailAddress}, review.Id);
			Assert.AreEqual("success", ((JsonResultDataModel) jsonResult.Data).status);
		}

		[Then]
		public void ThenTheReviewHasOnlyPeers_P0(string p0) {
			var peers = _context.DatabaseContext.ReviewConfigurations.Single().Peers;
			var peerNames = peers.Select(p => p.UserName).ToList();
			var me = _reviewInfo.CurrentUser;
			Assert.AreEqual(2, peers.Count);
			Assert.IsTrue(peerNames.Contains(me.UserName));
			Assert.IsTrue(peerNames.Contains(p0));
		}

		[Given(@"the review has category Speed")]
		public void GivenTheReviewHasCategorySpeed() {
			var review = _reviewInfo.ReviewConfiguration;
			review.Categories.Remove(review.Categories.First());
			review.Categories.Add(new ReviewCategory {Name = "Speed"});
			Assert.AreEqual(1, review.Categories.Count);
		}

		[Given(@"the review has categories (.*) and (.*)")]
		public void GivenTheReviewHasTwoCategories(string category1, string category2) {
			var review = _reviewInfo.ReviewConfiguration;
			review.Categories.Remove(review.Categories.First());
			review.Categories.Add(new ReviewCategory {Name = category1});
			review.Categories.Add(new ReviewCategory {Name = category2});
			Assert.AreEqual(2, review.Categories.Count);
		}

		[Given(@"three rounds of the review have been completed")]
		public void GivenThreeRoundsOfTheReviewHaveBeenCompleted() {
			var review = _reviewInfo.ReviewConfiguration;
			var reviewService = new ReviewService(_context.DatabaseContext, _fakeEmailService);
			for (int round = 0; round < 3; round++) {
				reviewService.StartReview(review.Id, review.Peers.First().EmailAddress);
				var feedbackService = new FeedbackService(_context.DatabaseContext);
				for (int i = 0; i < review.Peers.Count; i++) {
					feedbackService.SaveFeedback(review.Peers[i].EmailAddress, GetFeedback(review, (i + 5)%10 + 1));
				}
			}
		}

		[Given(@"the following assessments have been made")]
		public void GivenTheFollowingAssessmentsHaveBeenMade(IEnumerable<AssessmentInfo> givenAssessments) {
			var assessmentInfos = givenAssessments as IList<AssessmentInfo> ?? givenAssessments.ToList();
			var review = _reviewInfo.ReviewConfiguration;
			var reviewService = new ReviewService(_context.DatabaseContext, _fakeEmailService);
			var feedbackService = new FeedbackService(_context.DatabaseContext);

			// first round
			reviewService.StartReview(review.Id, review.Peers.First().EmailAddress);
			foreach (var assessment in assessmentInfos) {
				SaveFeedback(feedbackService, review, assessment, assessment.PerformanceRound1, "performance");
				SaveFeedback(feedbackService, review, assessment, assessment.ProductivityRound1, "productivity");
			}

			// second round
			reviewService.StartReview(review.Id, review.Peers.First().EmailAddress);
			foreach (var assessment in assessmentInfos) {
				SaveFeedback(feedbackService, review, assessment, assessment.PerformanceRound2, "performance");
				SaveFeedback(feedbackService, review, assessment, assessment.ProductivityRound2, "productivity");
			}
		}

		private static void SaveFeedback(IFeedbackService feedbackService, ReviewConfiguration review,
		                                 AssessmentInfo assessment, int rating, string category) {
			var userNameReviewer = assessment.Reviewer == "(me)" ? DefaultUserName : assessment.Reviewer;
			var userNameReviewedPeer = assessment.ReviewedPeer == "(me)" ? DefaultUserName : assessment.ReviewedPeer;

			feedbackService.SaveFeedback(review.Peers.First(p => p.UserName == userNameReviewer).EmailAddress,
			                             GetFeedback(review, rating, category,
													 review.Peers.First(p => p.UserName == userNameReviewedPeer)));
		}

		private static FeedbackViewModel GetFeedback(ReviewConfiguration review, int rating,
		                                             string categoryName = "Speed", UserProfile reviewedPeer = null) {
			ReviewCategory category = review.Categories.First(cat => cat.Name == categoryName);
			var feedback = new FeedbackViewModel
				               {
					               ReviewId = review.Id,
					               CategoriesWithPeersAndRatings =
						               {
							               new CategoryWithPeersAndRatings
								               {
									               Category = new CategoryShowModel {Id = category.Id, Name = category.Name},
									               PeersWithRatings = reviewedPeer != null
										                                  ? new List<PeerWithRating>
											                                    {
												                                    new PeerWithRating
													                                    {
														                                    Peer = new PeerShowModel
															                                           {
																                                           Id = reviewedPeer.Id,
																                                           UserName = reviewedPeer.UserName,
																                                           EmailAddress = reviewedPeer.EmailAddress
															                                           },
														                                    Rating = rating
													                                    }
											                                    }
										                                  : review.Peers.Select(
											                                  peer => new PeerWithRating
												                                          {
													                                          Peer = new PeerShowModel
														                                                 {
															                                                 Id = peer.Id,
															                                                 UserName = peer.UserName,
															                                                 EmailAddress = peer.EmailAddress
														                                                 },
													                                          Rating = rating
												                                          }).ToList()
								               }
						               }
				               };
			return feedback;
		}

		private static string CreateNewDatabase() {
			const string databaseFileName = "ReviewTestDatabase.sdf";

			string filePath = Path.Combine(Environment.CurrentDirectory, databaseFileName);
			if (File.Exists(filePath)) {
				File.Delete(filePath);
			}

			string connectionString = "Datasource = " + filePath;

			// NEED TO SET THIS TO MAKE DATABASE CREATION WORK WITH SQL CE!!!
			Database.DefaultConnectionFactory =
				new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");

			using (var context = new DatabaseContext(connectionString)) {
				context.Database.Create();
			}

			return connectionString;
		}

		private ReviewController CreateReviewControllerForCurrentUser() {
			UserProfile user = _reviewInfo.CurrentUser;
			Assert.IsNotNull(user, "The current user must not be null when creating a ReviewController instance!");
			Mock<ControllerContext> mockedControllerContext = GetMockedControllerContext(user.EmailAddress);
			return new ReviewController(_context.DatabaseContext, new ReviewService(_context.DatabaseContext, _fakeEmailService))
				       {
					       ControllerContext = mockedControllerContext.Object
				       };
		}

		private FeedbackController CreateFeedbackControllerForCurrentUser() {
			UserProfile user = _reviewInfo.CurrentUser;
			Assert.IsNotNull(user, "The current user must not be null when creating a FeedbackController instance!");
			Mock<ControllerContext> mockedControllerContext = GetMockedControllerContext(user.EmailAddress);
			return new FeedbackController(_context.DatabaseContext, new FeedbackService(_context.DatabaseContext))
				       {
					       ControllerContext = mockedControllerContext.Object
				       };
		}

		private ReportController CreateReportControllerForCurrentUser() {
			UserProfile user = _reviewInfo.CurrentUser;
			Assert.IsNotNull(user, "The current user must not be null when creating a FeedbackController instance!");
			Mock<ControllerContext> mockedControllerContext = GetMockedControllerContext(user.EmailAddress);
			return new ReportController(_context.DatabaseContext, new ReportService(_context.DatabaseContext))
				       {
					       ControllerContext = mockedControllerContext.Object
				       };
		}

		private static Mock<ControllerContext> GetMockedControllerContext(string userEmailAddress) {
			var mockedControllerContext = new Mock<ControllerContext>();
			mockedControllerContext.SetupGet(p => p.HttpContext.User.Identity.Name).Returns(userEmailAddress);
			mockedControllerContext.SetupGet(p => p.HttpContext.Request.IsAuthenticated).Returns(true);
			return mockedControllerContext;
		}
	}
}