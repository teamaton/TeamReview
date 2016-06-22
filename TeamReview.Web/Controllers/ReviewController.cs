using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using AutoMapper;
using Newtonsoft.Json;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;
using TeamReview.Core.Services;
using TeamReview.Web.Filters;
using TeamReview.Web.ViewModels;

namespace TeamReview.Web.Controllers {
	[Authorize]
	[AccessControl(actionNamesToIgnore: new[] { "Create", "Index", "CheckForNewReviewRounds" })]
	[HttpNotFoundIfInvalidId]
	public class ReviewController : Controller {
		private readonly IDatabaseContext _db;
		private readonly IReviewService _reviewService;

		public ReviewController(IDatabaseContext dbContext, IReviewService reviewService) {
			_db = dbContext;
			_reviewService = reviewService;
		}

		public ActionResult Index() {
			var currentUserId = _db.UserProfiles.First(user => user.EmailAddress == User.Identity.Name).Id;
			var reviewConfigurations = _db.ReviewConfigurations
				.Where(r => r.Peers.Any(p => p.Id == currentUserId))
				.ToList();
			var reviewViewModels = new List<ReviewViewModel>();
			foreach (var reviewConfiguration in reviewConfigurations) {
				var reviewViewModel = new ReviewViewModel
					                      { ReviewId = reviewConfiguration.Id, Name = reviewConfiguration.Name };
				if (!reviewConfiguration.ReviewRounds.Any(round => round.Active)) {
					reviewViewModel.ActionStatus = ActionStatus.NotStarted;
				}
				else if (reviewConfiguration.GetCurrentFeedback().Count() == reviewConfiguration.Peers.Count) {
					reviewViewModel.ActionStatus = ActionStatus.ShowResults;
				}
				else if (reviewConfiguration.GetCurrentFeedback().Any(f => f.Reviewer.Id == currentUserId)) {
					reviewViewModel.ActionStatus = ActionStatus.WaitForReviews;
				}
				else {
					reviewViewModel.ActionStatus = ActionStatus.ProvideReview;
				}
				reviewViewModels.Add(reviewViewModel);
			}
			return View(reviewViewModels);
		}

		public ActionResult Details(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			return View(reviewconfiguration);
		}

		[AllowAnonymous]
		public ActionResult CheckForNewReviewRounds() {
			_reviewService.StartNewReviewRounds();
			return Content("OK");
		}

		[HttpGet]
		public ActionResult Create() {
			return View(new ReviewCreateEditModel());
		}

		[HttpPost]
		[FormValueRequired("submit")]
		public ActionResult Create(ReviewCreateEditModel reviewCreateModel, string submit) {
			// manual model validation
			ValidateModel(reviewCreateModel);

			if (!ModelState.IsValid) {
				return View(reviewCreateModel);
			}

			// TODO: raise model state errors for duplicate email addresses

			var newReview = Mapper.Map<ReviewConfiguration>(reviewCreateModel);
			newReview.EnsureName();

			_db.ReviewConfigurations.Add(newReview);

			foreach (var cat in reviewCreateModel.AddedCategories.Select(Mapper.Map<ReviewCategory>)) {
				newReview.Categories.Add(cat);
			}

			foreach (var newPeer in reviewCreateModel.AddedPeers.Select(Mapper.Map<UserProfile>)) {
				var fromDb = _db.UserProfiles.SingleOrDefault(userProfile => userProfile.EmailAddress == newPeer.EmailAddress);
				newReview.Peers.Add(fromDb ?? newPeer);
			}

			var loggedInUser = _db.UserProfiles.FirstOrDefault(userProfile => userProfile.EmailAddress == User.Identity.Name);
			if (loggedInUser != null) {
				newReview.Peers.Add(loggedInUser);
			}

			_db.SaveChanges();

			if (submit == "Save and Start the Review") {
				// there will be a redirect at the end of StartReview
				return StartReview(newReview.Id);
			}

			TempData["Message"] = "Review has been created";

			return RedirectToAction("Index");
		}

		[HttpGet]
		public ActionResult Edit(int id = 0) {
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.SingleOrDefault(rev => rev.Id == id);
			return View("Create", Mapper.Map<ReviewCreateEditModel>(reviewFromDb));
		}

		[HttpPost, ActionName("Edit")]
		[FormValueRequired("submit")]
		public ActionResult EditPost(string submit, int id = 0) {
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.Single(rev => rev.Id == id);

			var reviewEditModel = Mapper.Map<ReviewCreateEditModel>(reviewFromDb);
			// load form data into new model object
			UpdateModel(reviewEditModel);

			// manual model validation
			ValidateModel(reviewEditModel);

			if (!ModelState.IsValid) {
				return View("Create", reviewEditModel);
			}

			reviewFromDb.Name = reviewEditModel.Name;
			reviewFromDb.EnsureName();
			// use this if there are more properties to set
			// _db.Entry(reviewFromDb).CurrentValues.SetValues(reviewEditModel);

			foreach (var cat in reviewEditModel.AddedCategories.Select(Mapper.Map<ReviewCategory>)) {
				reviewFromDb.Categories.Add(cat);
			}

			foreach (var newPeer in reviewEditModel.AddedPeers.Select(Mapper.Map<UserProfile>)) {
				var fromDb = _db.UserProfiles.SingleOrDefault(user => user.EmailAddress == newPeer.EmailAddress);
				reviewFromDb.Peers.Add(fromDb ?? newPeer);
			}

			_db.SaveChanges();

			if (submit == "Save and Start the Review") {
				// there will be a redirect at the end of StartReview
				return StartReview(id);
			}

			TempData["Message"] = "Review has been saved";
			return RedirectToAction("Edit", new { id });
		}

		[HttpPost, ActionName("Create")]
		[FormValueRequired("submit.add")]
		public ActionResult CreateExtension(ReviewCreateEditModel reviewCreateModel) {
			var action = Request.Form["submit.add"];
			if (action != null) {
				if (action == "addCategory") {
					reviewCreateModel.AddedCategories.Add(new CategoryAddModel());
					return View("Create", reviewCreateModel);
				}
				if (action == "addPeer") {
					reviewCreateModel.AddedPeers.Add(new PeerAddModel());
					return View("Create", reviewCreateModel);
				}
			}
			// ReSharper disable once NotResolvedInText
			throw new ArgumentNullException("submit.add", "The given form field must not be empty!");
		}

		[HttpPost, ActionName("Edit")]
		[FormValueRequired("submit.add")]
		public ActionResult EditExtension(int id) {
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.Single(rev => rev.Id == id);

			var newModel = Mapper.Map<ReviewCreateEditModel>(reviewFromDb);

			return CreateExtension(newModel);
		}

		[HttpGet]
		public ActionResult Delete(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			return View(reviewconfiguration);
		}

		[HttpPost, ActionName("Delete")]
		public ActionResult DeleteConfirmed(int id) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			_db.ReviewConfigurations.Remove(reviewconfiguration);
			_db.SaveChanges();
			return RedirectToAction("Index");
		}

		[HttpPost]
		public JsonResult RemoveCategory(RemoveCategoryViewModel viewModel, int id) {
			if (id != viewModel.ReviewId) {
				throw new ArgumentException("id");
			}

			var response = new JsonResultDataModel();
			var reviewConfiguration = _db.ReviewConfigurations.Find(viewModel.ReviewId);
			if (reviewConfiguration == null) {
				return new JsonResult { Data = response.Status("error").Message("The requested review could not be found!") };
			}
			var category = reviewConfiguration.Categories.SingleOrDefault(cat => cat.Id == viewModel.CategoryId);
			if (category == null) {
				return new JsonResult { Data = response.Status("error").Message("The desired category could not be found") };
			}
			var success = reviewConfiguration.Categories.Remove(category);
			_db.SaveChanges();

			return new JsonResult
				       {
					       Data = response
						       .Status(success ? "success" : "error")
						       .Message(success
							                ? "The category was successfully removed."
							                : "An error occurred while removing the category!")
				       };
		}

		[HttpPost]
		public JsonResult RemovePeer(RemovePeerViewModel viewModel, int id) {
			if (id != viewModel.ReviewId) {
				throw new ArgumentException("id");
			}

			var response = new JsonResultDataModel();
			var reviewConfiguration = _db.ReviewConfigurations.Find(viewModel.ReviewId);
			if (reviewConfiguration == null) {
				return new JsonResult { Data = response.Status("error").Message("The requested review could not be found!") };
			}
			var peer = reviewConfiguration.Peers.SingleOrDefault(user => user.EmailAddress == viewModel.PeerEmail);
			if (peer == null) {
				return new JsonResult { Data = response.Status("error").Message("The desired peer could not be found") };
			}
			var success = reviewConfiguration.Peers.Remove(peer);
			_db.SaveChanges();

			return new JsonResult
				       {
					       Data = response
						       .Status(success ? "success" : "error")
						       .Message(success ? "The peer was successfully removed." : "An error occurred while removing the peer!")
				       };
		}

		[HttpGet]
		public ActionResult Results(int id = 0) {
			var review = _db.ReviewConfigurations.Find(id);
			var myId = _db.UserProfiles.First(user => user.EmailAddress == User.Identity.Name).Id;

			Func<object, string> toJson = JsonConvert.SerializeObject;

			//_db.Entry(review).Collection(c => c.Feedback).Load();
			//_db.Entry(review).Collection(c => c.Categories).Load();
			//_db.Entry(review).Collection(c => c.Peers).Load();
			//foreach (var reviewFeedback in review.Feedback) {
			//    _db.Entry(reviewFeedback).Collection(f => f.Assessments).Load();
			//}
			var reviewers = review.GetCurrentFeedback().Select(f => f.Reviewer);

			var results = new ResultViewModel
				              {
					              ReviewId = review.Id,
					              ReviewName = review.Name,
					              Peers = review.Peers,
					              Reviewers = reviewers,
					              CategoriesJson = toJson(review.Categories.Select(cat => cat.Name)),
					              PeersJson = toJson(review.Peers.Names()),
				              };

			// my results
			var myResults = new
				                {
					                byMe =
						                review.Categories.Select(cat => review.GetCurrentFeedback().GetOwnRatingForCategory(myId, cat)),
					                byPeers =
						                review.Categories.Select(
							                cat => review.GetCurrentFeedback().GetPeerRatingForPeerForCategory(myId, cat))
				                };
			results.MyResultsJson = toJson(myResults);

			// all peer results
			var peerResultsPerCategory =
				review.Peers.Select(
					peer => review.Categories.Select(
						cat => review.GetCurrentFeedback().GetPeerRatingForPeerForCategory(peer.Id, cat)));
			results.PeerRatingsPerCategoryJson = toJson(peerResultsPerCategory);

			// results for stacked ranking
			var categoryResultsPerPeer =
				review.Categories
					.Select(cat => review.Peers
						               .Select(peer => review.GetCurrentFeedback().GetPeerRatingForPeerForCategory(peer.Id, cat))
						               .ToList())
					.ToList();

			// add fake value for each peer as last point in series for sum labels to show
			categoryResultsPerPeer.Add(review.Peers.Select(peer => 0.001m).ToList());
			results.CategoryResultsPerPeerJson = toJson(categoryResultsPerPeer);

			// sum labels
			var sums = peerResultsPerCategory.Select(peerResults => string.Format("∑ {0:#.##}", peerResults.Sum())).ToList();
			results.StackRankingSumLabels = toJson(sums);

			return View(results);
		}

		[HttpPost]
		public ActionResult StartReview(int id = 0) {
			_reviewService.StartReview(id, User.Identity.Name);

			TempData["Message"] = "Review has been started and mails have been sent to peers";
			return RedirectToAction("Index");
		}

		private void ValidateModel(ReviewCreateEditModel reviewCreateModel) {
			// 1. Remove empty entries from categories and peers
			for (var index = 0; index < reviewCreateModel.AddedCategories.Count; index++) {
				var category = reviewCreateModel.AddedCategories[index];
				if (string.IsNullOrWhiteSpace(category.Name) && string.IsNullOrWhiteSpace(category.Description)) {
					reviewCreateModel.AddedCategories.Remove(category);
				}
			}
			for (var index = 0; index < reviewCreateModel.AddedPeers.Count; index++) {
				var peer = reviewCreateModel.AddedPeers[index];
				if (string.IsNullOrWhiteSpace(peer.UserName) && string.IsNullOrWhiteSpace(peer.EmailAddress)) {
					reviewCreateModel.AddedPeers.Remove(peer);
				}
			}
			// 2. Check for categories with only a description
			for (var i = 0; i < reviewCreateModel.AddedCategories.Count; i++) {
				var index = i;
				var category = reviewCreateModel.AddedCategories[index];
				if (string.IsNullOrWhiteSpace(category.Name)) {
					Expression<Func<ReviewCreateEditModel, string>> expression = x => x.AddedCategories[index].Name;
					var key = ExpressionHelper.GetExpressionText(expression);
					ModelState.AddModelError(key, "Please give your category a name.");
				}
			}
			// 3. Check for peers without one of the fields
			for (var i = 0; i < reviewCreateModel.AddedPeers.Count; i++) {
				var index = i;
				var peer = reviewCreateModel.AddedPeers[index];
				if (string.IsNullOrWhiteSpace(peer.UserName)) {
					Expression<Func<ReviewCreateEditModel, string>> expression = x => x.AddedPeers[index].UserName;
					var key = ExpressionHelper.GetExpressionText(expression);
					ModelState.AddModelError(key, "Please enter your peer's name.");
				}
				else if (string.IsNullOrWhiteSpace(peer.EmailAddress)) {
					Expression<Func<ReviewCreateEditModel, string>> expression = x => x.AddedPeers[index].EmailAddress;
					var key = ExpressionHelper.GetExpressionText(expression);
					ModelState.AddModelError(key, "Please enter your peer's email address.");
				}
			}
		}
	}
}