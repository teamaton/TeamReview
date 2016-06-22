using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Coypu;
using Coypu.Drivers;
using Massive;
using NUnit.Framework;
using OpenQA.Selenium;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;
using TechTalk.SpecFlow;

namespace TeamReview.Specs {
	[Binding]
	public class Steps {
		private const int _port = 12345;
		private static IisExpressProcess _iisExpress;
		private static BrowserSession _browser;
		private static SeleniumServerProcess _seleniumServer;

		private static readonly string WebPath = Path.Combine(
			new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.FullName, "TeamReview.Web");

		private static string _dbPath;

		private static readonly string ConnectionString = string.Format("Data Source={0};Persist Security Info=False;", DbPath);
	    private string _emailDomain = "@my-domain.com";

	    private static string DbPath {
			get {
				return _dbPath ??
				       (_dbPath = Path.Combine(WebPath, "App_Data", GetDbName()));
			}
		}

		private static string DbBkpPath {
			get { return DbPath + ".bak"; }
		}

		[BeforeTestRun]
		public static void BeforeTestRun() {
			Assert.That(Directory.Exists(WebPath), WebPath + " not found!");

			BackupExistingDatabase();

			AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetFullPath(Path.Combine(WebPath, "App_Data")));

			_seleniumServer = new SeleniumServerProcess();
			_seleniumServer.Start();

			var sessionConfiguration = new SessionConfiguration
			                           	{
			                           		AppHost = "localhost",
			                           		Port = _port,
			                           		Browser = Browser.Firefox,
			                           		//Browser = Browser.HtmlUnitWithJavaScript,
			                           		Timeout = TimeSpan.FromSeconds(15),
			                           		RetryInterval = TimeSpan.FromSeconds(1),
			                           	};
			_browser = new BrowserSession(sessionConfiguration);
		}

		[BeforeScenario]
		public void BeforeScenario() {
			_iisExpress = new IisExpressProcess(WebPath, _port);
			_iisExpress.Start();
			// delete cookies on current page
			DeleteAllCookies();
			// delete Google cookies
			LogOutGoogle();
		}

		private static void DeleteAllCookies() {
			((IWebDriver) _browser.Driver.Native).Manage().Cookies.DeleteAllCookies();
		}

		[AfterScenario]
		public void AfterScenario() {
			var testError = ScenarioContext.Current.TestError;
			if (testError != null) {
				Console.WriteLine(testError.Message);
				Console.WriteLine(testError.StackTrace);
				Console.WriteLine(testError.InnerException);
				Console.WriteLine("Error Html:" + Environment.NewLine +
				                  ((IWebDriver) _browser.Driver.Native).PageSource);
#if DEBUG
				// needs to be closed manually
				ProcessHelper.StartInteractive("cmd").WaitForExit();
#endif
			}
			_iisExpress.Dispose();
			DeleteTestDatabase();
		}

		[AfterTestRun]
		public static void AfterTestRun() {
			RestoreExistingDatabase();
			try {
				_browser.Dispose();
			}
			catch {
			}
			try {
				_iisExpress.Dispose();
			}
			catch {
			}
			try {
				_seleniumServer.Dispose();
			}
			catch {
			}
		}

		[Given(@"I navigate to the homepage")]
		public void GivenINavigateToTheHomepage() {
			_browser.Visit("/");
			Assert.That(_browser.Location.AbsolutePath, Is.EqualTo("/"));
		}

		[Then(@"I should find <(.*)> on page")]
		public void ThenIShouldFindOnPage(string htmlElement) {
			Assert.That(_browser.HasCss(htmlElement));
		}

		[Given(@"I own a Google account")]
		public void GivenIOwnAGoogleAccount() {
            // TODO: add your email credentials here
			ScenarioContext.Current.Set(
				new Email {Address = "", Password = ""});
		}

		[StepDefinition(@"I am not logged into TeamReview")]
		public void GivenIAmNotLoggedIntoTeamReview() {
			// in case we haven't visited any other resource yet
			Console.WriteLine("Currently on " + _browser.Location);
			Console.WriteLine("Checking '{0}' vs. '{1}'", _browser.Location.Host, _iisExpress.BaseUrl);
			if (_browser.Location.Host != _iisExpress.BaseUrl) {
				_browser.Visit("/");
			}
			if (_browser.HasCss("#logoffLink")) {
				WhenILogOut();
			}
			Assert.That(_browser.HasCss("#loginLink"), "Could not find login link!");
		}

		[When(@"I register a new account")]
		public void WhenIRegisterANewAccount() {
			_browser.Visit("/Account/Register");
		}

		[When(@"I use my Google account")]
		public void WhenIRegisterWithMyGoogleAccount() {
			_browser.ClickButton("Google");

			// Google login page
			var email = ScenarioContext.Current.Get<Email>();
			_browser.FillIn("Email").With(email.Address);
			_browser.FillIn("Passwd").With(email.Password);
			_browser.Uncheck("PersistentCookie"); // don't remember me
			_browser.FindId("signIn").Click(); // sign in to Google

			// Google OpenID acceptance page: to revoke access, go to: https://www.google.com/accounts/b/0/IssuedAuthSubTokens
			var notApprovedYet = new State(() => _browser.Has(_browser.FindId("submit_approve_access")));
			var approved = new State(() => _browser.HasNo(_browser.FindId("submit_approve_access")));
			if (_browser.FindState(notApprovedYet, approved) == notApprovedYet) {
				_browser.FindId("submit_approve_access").Click(); // authenticate using Google
			}
		}

		public void WhenILogInWithMyGoogleAccount() {
			_browser.ClickButton("Google");

			// Google OpenID acceptance page
			_browser.Uncheck("remember_choices_checkbox"); // don't remember my choice
			_browser.FindId("approve_button").Click(); // authenticate using Google
		}

		[When(@"I fill in a user name")]
		public void WhenIFillInMyUserName() {
			_browser.FillIn("UserName").With("Tester");
		}

		[When(@"I finish registering")]
		public void WhenIFinishRegistering() {
			_browser.FindId("Register").Click();
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving user from DB");
				ScenarioContext.Current.Set(ctx.UserProfiles.Single());
			}
		}

		[When(@"I finish login")]
		public void WhenIFinishLogin() {
			_browser.FindId("Login").Click();
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving user from DB");
				ScenarioContext.Current.Set(ctx.UserProfiles.Single());
			}
		}

		[Then(@"a new account was created with my Google address")]
		public void ThenANewAccountWasCreatedWithMyGoogleAddress() {
			var emailAddress = ScenarioContext.Current.Get<Email>().Address;
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving user from DB");
				Assert.That(ctx.UserProfiles.Single().EmailAddress, Is.EqualTo(emailAddress));
			}
		}

		[Then(@"I am logged in")]
		public void ThenIAmLoggedIn() {
			Assert.That(_browser.HasCss("#logoffLink"));
		}

		[Then(@"I am on the ""(.*)""")]
		public void ThenIAmOnThe(string pageName) {
			string path;
			switch (pageName) {
				case "Dashboard":
					path = "/dashboard";
					break;
				case "Reviews":
					path = "/Review";
					break;
				default:
					throw new ArgumentOutOfRangeException("pageName", "No mapping from '{0}' to concrete url path exists!");
			}
			Assert.That(_browser.Location.AbsolutePath, Is.EqualTo(path));
		}

		[When(@"I create a new review")]
		public void WhenICreateANewReview() {
			ScenarioContext.Current.Set(new ReviewConfiguration {Peers = {ScenarioContext.Current.Get<UserProfile>()}});
			_browser.Visit("/Review/Create");
		}

		[When(@"I edit my review")]
		public void WhenIEditMyReview() {
			var reviewId = ScenarioContext.Current.Get<ReviewConfiguration>().Id;
			_browser.Visit("/Review/Edit/" + reviewId);
		}

		[Given(@"I am logged in")]
		public void GivenIAmLoggedIn() {
			GivenIOwnAGoogleAccount();
			WhenIRegisterUsingMyGoogleAccount();
			WhenIFinishRegistering();
		}

		[Given(@"I own a review")]
		[Given(@"I am invited to a review")]
		public void GivenIOwnAReview() {
			GivenIOwnAReview("Untitled Review");
		}

		private void GivenIOwnAReview(string reviewName) {
			var thisIsMe = ScenarioContext.Current.Get<UserProfile>();
			var reviewConfiguration = new ReviewConfiguration {Name = reviewName, LengthOfReviewRoundInWeeks = 1};
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Writing review to DB");
				reviewConfiguration.Peers.Add(ctx.UserProfiles.Find(thisIsMe.Id));
				ctx.ReviewConfigurations.Add(reviewConfiguration);
				ctx.SaveChanges();
				ScenarioContext.Current.Set(reviewConfiguration);
			}
		}

		[Given(@"I own a review (.*)")]
		public void GivenIOwnAReviewWithName(string reviewName) {
			GivenIOwnAReview(reviewName);
		}

		[Given(@"I am invited to a review (.*)")]
		public void GivenIAmInvitedToAReviewWithName(string reviewName) {
			GivenIOwnAReview(reviewName);
		}

		[Given(@"I am not part of review (.*)")]
		public void GivenIAmNotPartOfReview(string reviewName) {
			var reviewConfiguration = new ReviewConfiguration { Name = reviewName, LengthOfReviewRoundInWeeks = 1 };
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Writing review to DB");
				reviewConfiguration.Peers.Add(new UserProfile {EmailAddress = "test@example.com", UserName = "somebody else"});
				ctx.ReviewConfigurations.Add(reviewConfiguration);
				ctx.SaveChanges();
				ScenarioContext.Current.Set(reviewConfiguration);
			}
		}

		[Given(@"I own a review with two peers")]
		public void GivenIOwnAReviewWithTwoPeers() {
			var peers = new List<UserProfile>();
			peers.Add(ScenarioContext.Current.Get<UserProfile>());
			peers.Add(new UserProfile {EmailAddress = "Anton" + _emailDomain, UserName = "Anton"});
			peers.Add(new UserProfile {EmailAddress = "Admin" + _emailDomain, UserName = "Admin"});
			var reviewConfiguration = new ReviewConfiguration { Name = "NewReview", LengthOfReviewRoundInWeeks = 1, Peers = peers };
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Writing review to DB");
				ctx.ReviewConfigurations.Add(reviewConfiguration);
				ctx.SaveChanges();
			}
			ScenarioContext.Current.Set(reviewConfiguration);
		}

		[Given(@"I have a started review with two categories")]
		public void GivenIHaveAStartedReviewWithTwoCategories() {
			GivenIHaveAStartedReviewWithCategoriesAndAdditionalPeers(2, 0);
		}

		[Given(@"I have a started review with (.*) categories and (.*) additional peers")]
		public void GivenIHaveAStartedReviewWithCategoriesAndAdditionalPeers(int numberOfCategories, int numberOfPeers) {
			GivenIOwnAReview();
			var reviewId = ScenarioContext.Current.Get<ReviewConfiguration>().Id;
			using (var ctx = new DelayedDatabaseContext()) {
				var reviewConfiguration = ctx.ReviewConfigurations.Find(reviewId);
				for (var i = 0; i < numberOfCategories; i++) {
					reviewConfiguration.Categories.Add(new ReviewCategory {Name = "cat" + i, Description = "desc" + i});
				}
				for (var i = 0; i < numberOfPeers; i++) {
				    reviewConfiguration.Peers.Add(new UserProfile {EmailAddress = i + _emailDomain, UserName = "user" + i});
				}
			    reviewConfiguration.ReviewRounds.Add(new ReviewRound { Active = true, StartDate = DateTime.UtcNow });
				ctx.SaveChanges();
				ScenarioContext.Current.Set(reviewConfiguration);
			}
		}

		[Given(@"I have provided review")]
		public void GivenIHaveProvidedReview() {
			var reviewId = ScenarioContext.Current.Get<ReviewConfiguration>().Id;
			using (var ctx = new DelayedDatabaseContext()) {
				var reviewConfiguration = ctx.ReviewConfigurations.Find(reviewId);
				ctx.Entry(reviewConfiguration).Collection(c => c.Categories).Load();
				ctx.Entry(reviewConfiguration).Collection(c => c.Peers).Load();
				var newFeedback = new ReviewFeedback();
				var thisIsMyId = ScenarioContext.Current.Get<UserProfile>().Id;
				newFeedback.Reviewer = ctx.UserProfiles.Find(thisIsMyId);
				foreach (var reviewCategory in reviewConfiguration.Categories) {
					foreach (var peer in reviewConfiguration.Peers) {
						newFeedback.Assessments.Add(new Assessment
						                            	{
						                            		ReviewCategory = reviewCategory,
						                            		Rating = 7,
						                            		ReviewedPeer = peer,
						                            		Reviewer = newFeedback.Reviewer
						                            	});
					}
				}
				var reviewRound = new ReviewRound { Active = true, StartDate = DateTime.UtcNow};
				reviewRound.Feedback.Add(newFeedback);
				reviewConfiguration.ReviewRounds.Add(reviewRound);

				ctx.SaveChanges();
				ScenarioContext.Current.Set(reviewConfiguration);
			}
		}

		[Given(@"all peers have provided the review")]
		public void GivenAllPeersHaveProvidedTheReview() {
			var reviewId = ScenarioContext.Current.Get<ReviewConfiguration>().Id;
			using (var ctx = new DelayedDatabaseContext()) {
				var reviewConfiguration = ctx.ReviewConfigurations.Find(reviewId);
				ctx.Entry(reviewConfiguration).Collection(c => c.Categories).Load();
				ctx.Entry(reviewConfiguration).Collection(c => c.Peers).Load();
				foreach (var reviewer in reviewConfiguration.Peers) {
					var newFeedback = new ReviewFeedback();
					foreach (var reviewCategory in reviewConfiguration.Categories) {
						foreach (var peer in reviewConfiguration.Peers) {
							newFeedback.Assessments.Add(new Assessment
							                            	{
							                            		ReviewCategory = reviewCategory,
							                            		Rating = 7,
							                            		Reviewer = reviewer,
							                            		ReviewedPeer = peer
							                            	});
						}
					}
					newFeedback.Reviewer = reviewer;
					var reviewRound = new ReviewRound { Active = true, StartDate = DateTime.UtcNow };
					reviewRound.Feedback.Add(newFeedback);
					reviewConfiguration.ReviewRounds.Add(reviewRound);
				}
				ctx.SaveChanges();
				ScenarioContext.Current.Set(reviewConfiguration);
			}
		}

		[Given(@"I registered standardly")]
		public void GivenIRegisteredStandardly() {
			WhenIRegisterANewAccount();
			WhenIFillInMyEmailAddress("test@teamreview.net");
			WhenIFinishRegistering();
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving user from DB");
				ScenarioContext.Current.Set(ctx.UserProfiles.Single());
			}
		}

		[When(@"I follow link in validation email")]
		public void WhenIFollowLinkInValidationEmail() {
			var database = new DynamicModel("DefaultConnection", "UserProfile", "Id").SetConnectionString(ConnectionString);
			var confirmationToken = database.Query("SELECT ConfirmationToken FROM webpages_Membership").First().ConfirmationToken;
			var emailAddress = ScenarioContext.Current.Get<UserProfile>().EmailAddress;
			var url = "/Account/CompleteRegistration?confirmationToken=" + confirmationToken + "&email=" + emailAddress;
			Console.WriteLine(url);
			_browser.Visit(url);
		}

		[When(@"I enter UserName and Password twice")]
		public void WhenIEnterUserNameAndPasswordTwice() {
			_browser.FillIn("UserName").With("TestUser");
			_browser.FillIn("Password").With("TestPassword");
			_browser.FillIn("ConfirmPassword").With("TestPassword");
		}

		[Given(@"I have a standard account with email")]
		public void GivenIHaveAStandardAccountWithEmail() {
			GivenIRegisteredStandardly();
			WhenIFollowLinkInValidationEmail();
			WhenIEnterUserNameAndPasswordTwice();
			WhenIFinishRegistering();
		}

		[When(@"I enter UserName and Password")]
		public void WhenIEnterUserNameAndPassword() {
			_browser.FillIn("EmailAddress").With("test@teamreview.net");
			_browser.FillIn("Password").With("TestPassword");
		}


		[Given(@"I have an account at TeamReview")]
		public void GivenIHaveAnAccountAtTeamReview() {
			GivenIOwnAGoogleAccount();
			WhenIRegisterANewAccount();
			WhenIRegisterWithMyGoogleAccount();
			WhenIFillInMyUserName();
			WhenIFinishRegistering();
			WhenILogOut();
		}

		[When(@"I log in using my Google account")]
		public void WhenILogInUsingMyGoogleAccount() {
			_browser.Visit("/Account/Login");
			WhenILogInWithMyGoogleAccount();
		}

		[When(@"I register using my Google account")]
		public void WhenIRegisterUsingMyGoogleAccount() {
			_browser.Visit("/Account/Login");
			WhenIRegisterWithMyGoogleAccount();
		}

		[Given(@"I don't have an account at TeamReview")]
		public void GivenIDonTHaveAnAccountAtTeamReview() {
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving user from DB");
				if (!ctx.Database.Exists())
					Console.WriteLine("DB does not exist yet - no account exists");
				else
					Assert.That(ctx.UserProfiles.ToList(), Has.Count.EqualTo(0));
			}
		}

		[Given(@"I am logged into TeamReview")]
		public void GivenIAmLoggedIntoTeamReview() {
			GivenIAmLoggedIn();
		}

		/// <summary>
		/// Logs user out and deletes all Cookies.
		/// </summary>
		[When(@"I log out")]
		public void WhenILogOut() {
			LogOutGoogle();
			_browser.Visit("/");
			_browser.FindId("logoffLink").Click();
			DeleteAllCookies();
		}

		private static void LogOutGoogle() {
			_browser.Visit("https://www.google.com");
			DeleteAllCookies();
		}

		[Then(@"I am on the login page")]
		public void ThenIAmOnTheLoginPage() {
			Assert.That(_browser.Location.AbsolutePath, Is.EqualTo("/Account/Login"),
			            "Should be on the login page but am on " + _browser.Location);
		}

		[When(@"I fill in my email address ""(.*)""")]
		public void WhenIFillInMyEmailAddress(string emailAddress) {
			_browser.FillIn("emailAddress").With(emailAddress);
		}

		[When(@"I fill in a review name")]
		public void WhenIFillInAReviewName() {
			const string reviewName = "NewReview";
			ScenarioContext.Current.Get<ReviewConfiguration>().Name = reviewName;
			_browser.ExecuteScript(string.Format("$('#Name').val('{0}');", reviewName));
		}

		[When(@"I add (?:a|another) category")]
		public void WhenIAddACategory() {
			ScenarioContext.Current.Get<ReviewConfiguration>().Categories.Add(new ReviewCategory());
			_browser.ClickButton("addCategory");
		}

		[When(@"I fill in a category name")]
		public void WhenIFillInACategoryName() {
			var name = "cat-" + new Random().Next(100, 1000);
			ScenarioContext.Current.Get<ReviewConfiguration>().Categories.Last().Name = name;
			_browser.FindAllCss("#categories tr").Last().FillIn("Name").With(name);
		}

		[When(@"I fill in a category description")]
		public void WhenIFillInACategoryDescription() {
			var description = "desc-" + Guid.NewGuid();
			ScenarioContext.Current.Get<ReviewConfiguration>().Categories.Last().Description = description;
			_browser.FindAllCss("#categories tr").Last().FillIn("Description").With(description);
		}

		[When(@"I save the review")]
		public void WhenISaveTheReview() {
			_browser.ClickButton("Save");
		}

		[Then(@"my new review was created with those categories")]
		public void ThenMyNewReviewWasCreatedWithThoseCategories() {
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Categories").Single();
				Assert.AreEqual(review.Name, reviewFromDb.Name);
				Assert.AreEqual(review.Categories.Count, reviewFromDb.Categories.Count);
				Assert.That(reviewFromDb.Categories, Is.EqualTo(
					review.Categories).AsCollection.Using(new CategoryComparer()));
			}
		}

		[Then(@"my review is updated with the new category")]
		public void ThenMyReviewIsUpdatedWithTheNewCategory() {
			ThenMyNewReviewWasCreatedWithThoseCategories();
		}

		[Then(@"I am added to the review")]
		public void ThenIAmAddedToTheReview() {
			var thisIsMe = ScenarioContext.Current.Get<UserProfile>();
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Peers").Single();
				Assert.That(reviewFromDb.Peers.Count, Is.EqualTo(1));
				Assert.That(reviewFromDb.Peers[0].Id, Is.EqualTo(thisIsMe.Id));
			}
		}

		[Then(@"I am on the ""(.*)"" page for my review")]
		[Then(@"I am on the ""(.*)"" page for the review")]
		public void ThenIAmOnThePageForMyReview(string pagename) {
			Assert.IsTrue(_browser.Title.Contains(pagename));
		}

		[Then(@"I see the message ""(.*)""")]
		public void ThenISeeTheMessage(string message) {
			Assert.IsTrue(_browser.HasContent(message));
		}

		[Given(@"I am on the ""(.*)"" page")]
		[When(@"I go to the ""(.*)"" page")]
		public void GivenIAmOnThePage(string pageName) {
			string path;
			switch (pageName) {
				case "Dashboard":
					path = "/Review";
					break;
				case "Results":
					path = "/Review/Results/1";
					break;
				case "Login":
					path = "/Account/Login";
					break;
				default:
					throw new ArgumentOutOfRangeException("pageName", "No mapping from '" + pageName + "' to concrete url path exists!");
			}
			_browser.Visit(path);
		}

		[When(@"I click on the ""(.*)"" link of the review")]
		public void WhenIClickOnTheLinkOfTheReview(string linkName) {
			_browser.FindId("ReviewId_" + ScenarioContext.Current.Get<ReviewConfiguration>().Id).FindLink(linkName).Click();
		}

		[When(@"I invite a peer")]
		public void WhenIInviteAPeer() {
			_browser.ClickButton("addPeer");
			ScenarioContext.Current.Set(new UserProfile {UserName = "Peer", EmailAddress = "peer" + _emailDomain}, "peer");
		}

		[When(@"I fill in the peer's name")]
		public void WhenIFillInThePeerSName() {
			_browser.FillIn("UserName").With(ScenarioContext.Current.Get<UserProfile>("peer").UserName);
		}

		[When(@"I fill in the peer's email address")]
		public void WhenIFillInThePeerSEmailAddress() {
			_browser.FillIn("EmailAddress").With(ScenarioContext.Current.Get<UserProfile>("peer").EmailAddress);
		}

		[When(@"no account exists for that peer's email address")]
		public void WhenNoAccountExistsForThatPeerSEmailAddress() {
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Trying to retrieve peer from DB");
				var peerAddress = ScenarioContext.Current.Get<UserProfile>("peer").EmailAddress;
				var peerFromDb = ctx.UserProfiles.SingleOrDefault(user => user.EmailAddress == peerAddress);
				Assert.IsNull(peerFromDb);
			}
		}

		[Then(@"a new user with the given name and email address was created")]
		public void ThenANewUserWithTheGivenNameAndEmailAddressWasCreated() {
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Trying to retrieve peer from DB");
				var peer = ScenarioContext.Current.Get<UserProfile>("peer");
				var peerFromDb = ctx.UserProfiles.SingleOrDefault(
					user => user.EmailAddress == peer.EmailAddress && user.UserName == peer.UserName);
				Assert.IsNotNull(peerFromDb);
			}
		}

		[Then(@"this user is added to the review")]
		public void ThenThisUserIsAddedToTheReview() {
			var peer = ScenarioContext.Current.Get<UserProfile>("peer");

			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Peers").Single();
				Assert.AreEqual(1, reviewFromDb.Peers.Count(
					user => user.EmailAddress == peer.EmailAddress && user.UserName == peer.UserName));
			}
		}

		[When(@"an account exists for that peer's email address")]
		public void WhenAnAccountExistsForThatPeerSEmailAddress() {
			var peer = ScenarioContext.Current.Get<UserProfile>("peer");

			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Saving peer to DB");
				ctx.UserProfiles.Add(new UserProfile {UserName = peer.UserName, EmailAddress = peer.EmailAddress});
				ctx.SaveChanges();
			}
		}

		[When(@"I start the review")]
		public void WhenIStartTheReview() {
			GivenIAmOnThePage("Dashboard");
			_browser.FindId("ReviewId_" + ScenarioContext.Current.Get<ReviewConfiguration>().Id).FindLink("Start review").
				Click();
		}

		[Then(@"the review is active")]
		public void ThenTheReviewIsActive() {
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Single();
				Assert.IsTrue(reviewFromDb.ReviewRounds.Any(round => round.Active));
			}
		}

		[When(@"I visit the ""(.*)"" url")]
		public void WhenIVisitTheUrl(string url) {
			ScenarioContext.Current.Pending();
		}

		[When(@"I visit the Provide review url")]
		public void WhenIVisitTheProvideReviewUrl() {
			var reviewId = ScenarioContext.Current.Get<ReviewConfiguration>().Id;
			_browser.Visit("/Review/Provide/" + reviewId);
		}

		[When(@"I visit the Provide review url for (.+)")]
		public void WhenIVisitTheProvideReviewUrlFor(string reviewName) {
			ReviewConfiguration reviewFromDb;
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				reviewFromDb = ctx.ReviewConfigurations.SingleOrDefault(r => r.Name == reviewName);
				Assert.IsNotNull(reviewFromDb, "The review with name '{0}' could not be found in the DB!", reviewName);
			}
			_browser.Visit("/Review/Provide/" + reviewFromDb.Id);
		}

		[Then(@"I see for each category all peers")]
		public void ThenISeeForEachCategoryAllPeers() {
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			foreach (var category in review.Categories) {
				var elementScope = _browser.FindId("category_" + category.Id);
				Assert.IsTrue(elementScope.HasContent(category.Name));
				Assert.IsTrue(elementScope.HasContent(category.Description));
				foreach (var peer in review.Peers) {
					Assert.IsTrue(elementScope.HasContent(peer.UserName));
				}
			}
		}

		[Then(@"I see the name of review")]
		public void ThenISeeTheNameOfReview() {
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			Assert.IsTrue(_browser.HasContent(review.Name));
		}

		[Then(@"I see ""(.*)""")]
		public void ThenISee(string text) {
			Assert.IsTrue(_browser.HasContent(text));
		}

		[Then(@"I do not see ""(.*)""")]
		public void ThenIDoNotSee(string text) {
			Assert.IsFalse(_browser.HasContent(text));
		}

		[Then(@"I have input options from (.*) to (.*) for each peer for each category")]
		public void ThenIHaveInputOptionsFromToForEachForEachCategory(int start, int end) {
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			foreach (var category in review.Categories) {
				foreach (var peer in review.Peers) {
					var element = _browser.FindId("category_" + category.Id + "_peer_" + peer.Id);
					for (var i = start; i <= end; i++) {
						Assert.IsTrue(element.FindCss(@"input[type=""radio""][value=""" + i + @"""]").Exists());
						Assert.IsTrue(element.HasContent(i.ToString()));
					}
				}
			}
		}

		[When(@"I select (.*) for each category")]
		public void WhenISelectForEachCategory(int rating) {
			var radiobuttons = _browser.FindAllCss(@"input[type=""radio""][value=""" + rating + @"""]");
			foreach (var radiobutton in radiobuttons) {
				radiobutton.Click();
			}
		}

		[Then(@"the feedback is saved with (.*) for each peer for each category")]
		public void ThenTheFeedbackIsSavedWithForEachPeerForEachCategory(int rating) {
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Feedback").Single();
				var feedback = reviewFromDb.ReviewRounds.Last().Feedback.Single();
				foreach (var assessment in feedback.Assessments) {
					Assert.AreEqual(rating, assessment.Rating);
				}
			}
		}

		[Then(@"the feedback is not saved")]
		public void ThenTheFeedbackIsNotSaved() {
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Feedback").Single();
				Assert.IsFalse(reviewFromDb.ReviewRounds.Any(round => round.Feedback.Any()));
			}
		}

		[Then(@"I see ""(.*)"" for my review")]
		public void ThenISeeForMyReview(string text) {
			Assert.IsTrue(
				_browser.FindId("ReviewId_" + ScenarioContext.Current.Get<ReviewConfiguration>().Id).HasContent(text));
		}

		[Then(@"I do not see ""(.*)"" for my review")]
		public void ThenIDoNotSeeForMyReview(string text) {
			Assert.IsFalse(
				_browser.FindId("ReviewId_" + ScenarioContext.Current.Get<ReviewConfiguration>().Id).HasContent(text));
		}

		[Then(@"for each category I see the peer rating of me \(average rating of all peers except mine\) and my rating of me"
			)]
		public void ThenForEachCategoryISeeThePeerRatingOfMeAverageRatingOfAllPeersExceptMineAndMyRatingOfMe() {
			var yourResults = _browser.FindId("yourResults");
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			foreach (var category in review.Categories) {
				var elementScope = yourResults.FindId("catMyResults" + category.Name);
				Assert.IsTrue(elementScope.HasContent(category.Name));
				Assert.IsTrue(elementScope.HasContent(category.Description));
				Assert.IsTrue(elementScope.HasContent("Peer rating = 7"));
				Assert.IsTrue(elementScope.HasContent("My rating = 7"));
			}
		}

		[Then(@"for each category and each peer I see their peer rating \(average rating of all peers except his/hers\)")]
		public void ThenForEachCategoryAndEachPeerISeeTheirPeerRatingAverageRatingOfAllPeersExceptHisHers() {
			var everyonesResults = _browser.FindId("everyonesResults");
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			foreach (var category in review.Categories) {
				var elementScope = everyonesResults.FindId("catPeerResults" + category.Name);
				Assert.IsTrue(elementScope.HasContent(category.Name));
				Assert.IsTrue(elementScope.HasContent(category.Description));
				foreach (var peer in review.Peers) {
					Assert.IsTrue(elementScope.HasContent("Name = " + peer.UserName + " : peer rating = 7"));
				}
			}
		}

		[Then(@"I see the stacked rating of me \(sum of ratings of all categories\)")]
		public void ThenISeeTheStackedRatingOfMeSumOfRatingsOfAllCategories() {
			var yourResults = _browser.FindId("yourResults");
			Assert.IsTrue(yourResults.HasContent("Stacked rating by peers = 14"));
			Assert.IsTrue(yourResults.HasContent("Stacked rating by yourself = 14"));
		}

		[Then(@"I see the stacked rating of each peer \(sum of ratings of all categories\)")]
		public void ThenISeeTheStackedRatingOfEachPeerSumOfRatingsOfAllCategories() {
			var everyonesStackedResults = _browser.FindId("everyonesStackedResults");
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			foreach (var peer in review.Peers) {
				Assert.IsTrue(everyonesStackedResults.HasContent("Name = " + peer.UserName + " : peer rating = 14"));
			}
		}

		[Then(@"the review is not saved")]
		public void ThenTheReviewIsNotSaved() {
			// works only when editing a review, for now
			// checking against the default review: see GivenIOwnAReview()
			var thisIsMe = ScenarioContext.Current.Get<UserProfile>();
			using (var ctx = new DelayedDatabaseContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Categories").Include("Peers").Single();
				Assert.AreEqual("Untitled Review", reviewFromDb.Name);
				Assert.AreEqual(thisIsMe.EmailAddress, reviewFromDb.Peers[0].EmailAddress);
				CollectionAssert.IsEmpty(reviewFromDb.Categories);
			}
		}

		[Then(@"access is denied")]
		public void ThenAccessIsDenied() {
			Assert.That(_browser.HasContent("You don't have permission to access this page."));
		}

		#region Helpers

		private static void BackupExistingDatabase() {
			if (!File.Exists(DbPath)) {
				Console.WriteLine(string.Format("INFO: {0} does not exist", DbPath));
			}
			else {
				// second backup copy with timestamp in case something goes wrong
				File.Copy(DbPath, DbPath + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true);
				// move db file, but with overwrite = true
				File.Copy(DbPath, DbBkpPath, true);
				File.Delete(DbPath);
			}
		}

		private static void DeleteTestDatabase() {
			if (File.Exists(DbPath)) {
				File.Delete(DbPath);
			}
		}

		private static void RestoreExistingDatabase() {
			DeleteTestDatabase();
			if (File.Exists(DbBkpPath)) {
				Console.WriteLine("Restoring original DB from Backup.");
				File.Move(DbBkpPath, DbPath);
				Assert.That(File.Exists(DbPath),
				            "The original DB file should have been restored but hasn't been! Try to restore it manually (in TeamReview.Web\\App_Data).");
			}
			else {
				Assert.That(!File.Exists(DbPath),
				            "The original DB file should not exist since it didn't exist when Specs were started!");
			}
		}

		private static string GetDbName() {
			var webConfigPath = Path.Combine(WebPath, "web.config");
			Assert.That(File.Exists(webConfigPath), string.Format("The web.config could not be found in '{0}'.", WebPath));
			var dbNameMarker = @"connectionString=""Data Source=|DataDirectory|\";
			var webConfigText = File.ReadAllText(webConfigPath);
			var dbNameIndex = webConfigText.IndexOf(dbNameMarker) + dbNameMarker.Length;
			Assert.That(dbNameIndex, Is.GreaterThan(dbNameMarker.Length),
			            "The database name could not be found in the web.config! Please provide one in the connectionString.");
			var dbNameLength = webConfigText.IndexOf(';', dbNameIndex) - dbNameIndex;
			return webConfigText.Substring(dbNameIndex, dbNameLength);
		}

		#endregion
	}

	public class DelayedDatabaseContext : DatabaseContext {
		public DelayedDatabaseContext() {
			Thread.Sleep(1000);
		}
	}

	internal class Email {
		public string Address { get; set; }
		public string Password { get; set; }
	}

	internal class CategoryComparer : IEqualityComparer<ReviewCategory> {
		#region IEqualityComparer<ReviewCategory> Members

		public bool Equals(ReviewCategory x, ReviewCategory y) {
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return x.Name == y.Name && x.Description == y.Description;
		}

		public int GetHashCode(ReviewCategory obj) {
			return obj.GetHashCode();
		}

		#endregion
	}
}