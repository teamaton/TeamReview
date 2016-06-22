using System;
using System.Collections.Generic;
using System.Linq;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;

namespace TeamReview.Core.Services {
	public class ReportService : IReportService {
		private readonly IDatabaseContext _db;

		public ReportService(IDatabaseContext databaseContext) {
			_db = databaseContext;
		}

		public Report CreatePeerReport(int reviewId, IList<int> selectedUserIds, int categoryId) {
			var review = RetrieveValidReviewOrFail(reviewId);
			var category = RetrieveValidCategoryOrFail(categoryId, review);

			// select all users if no user was selected
			if (selectedUserIds == null || selectedUserIds.Count == 0) {
				selectedUserIds = review.Peers.Select(p => p.Id).ToList();
			}

			var peerNames = GetCommaSeparatedUserNames(selectedUserIds, review);
			var title = string.Format(@"Peer report - ""{0}"" ratings for {1}", category.Name, peerNames);

			var report = CreateReport(title, review, category);

			foreach (var id in selectedUserIds) {
				var selectedUser = RetrieveValidPeerOrFail(id, review);
				report.ReportData.Add(TeamRatingsOverTime(review.ReviewRoundsInOrder, selectedUser, category));	
			}

			return report;
		}

		private static string GetCommaSeparatedUserNames(IEnumerable<int> selectedUserIds, ReviewConfiguration review) {
			var userNames = "";
			foreach (var id in selectedUserIds) {
				if (userNames != "") {
					userNames += ", ";
				}
				userNames += RetrieveValidPeerOrFail(id, review).UserName;
			}
			return userNames;
		}

		public Report CreateOwnReport(int reviewId, string loggedInUserEmail, int categoryId) {
			var review = RetrieveValidReviewOrFail(reviewId);
			var category = RetrieveValidCategoryOrFail(categoryId, review);

			var title = string.Format(@"My own report - ""{0}"" ratings", category.Name);
			var report = CreateReport(title, review, category);

			var selectedUser = review.Peers.First(p => p.EmailAddress == loggedInUserEmail);
			report.ReportData.Add(OwnRatingsOverTime(review.ReviewRoundsInOrder, selectedUser, category));
			report.ReportData.Add(TeamRatingsOverTime(review.ReviewRoundsInOrder, selectedUser, category));

			return report;
		}

		public Report CreateTeamReport(int reviewId, int categoryId) {
			var review = RetrieveValidReviewOrFail(reviewId);
			var category = RetrieveValidCategoryOrFail(categoryId, review);

			var title = string.Format(@"Team report - ""{0}"" ratings", category.Name);

			var report = CreateReport(title, review, category);

			report.ReportData.Add(TeamRatingsOverTime(review.ReviewRoundsInOrder, null, category));
			return report;
		}

		private static Report CreateReport(string title, ReviewConfiguration review, ReviewCategory category) {
			return new Report {
				CategoryCount = 1,
				Title = title,
				ReviewName = review.Name,
				Description = category.Description,
				XAxisLabels = review.ReviewRoundsInOrder.Select(round => round.StartDate.ToShortDateString()).ToList()
			};
		}

		public Report CreateOwnStackReport(int reviewId, string loggedInUserEmail) {
			var review = RetrieveValidReviewOrFail(reviewId);
			var selectedUser = review.Peers.First(p => p.EmailAddress == loggedInUserEmail);
			var myRatingReport = GetCategoriesStackRating(reviewId, selectedUser.Id, loggedInUserEmail, true);
			var teamRatingReport = GetCategoriesStackRating(reviewId, selectedUser.Id, loggedInUserEmail, false);

			// insert the team ratings data between my own ratings
			if (review.ReviewRounds.Any()) {
				for (var i = 0; i < review.ReviewRounds.Count; i++) {
					myRatingReport.XAxisLabels.Insert(2 * i + 1, "(Team)");
					for (var j = 0; j < myRatingReport.ReportData.Count; j++) {
						myRatingReport.ReportData[j].Values.Insert(2 * i + 1,
							teamRatingReport.ReportData[j].Values[i]);
					}
				}	
			}

			myRatingReport.Title = "My own stack ratings and me being rated by the team";

			return myRatingReport;
		}

		private Report GetCategoriesStackRating(int reviewId, int peerId, string loggedInUserEmail, bool myRatingsOnly = false) {
			var review = RetrieveValidReviewOrFail(reviewId);
			var peer = RetrieveValidPeerOrFail(peerId, review);
			VerifyIntegrityOfLoggedInUser(loggedInUserEmail, myRatingsOnly, peer);

			var report = new Report
				             {
					             Title = myRatingsOnly
						                     ? "My own Stack Ratings"
						                     : string.Format(@"Stack Ratings for ""{0}""", peer.UserName),
					             Description = "A stack of ratings in all categories",
					             ReviewName = review.Name,
					             CategoryCount = review.Categories.Count,
					             XAxisLabels = review.ReviewRoundsInOrder.Select(round => round.StartDate.ToShortDateString()).ToList()
				             };

			// x-axis: time-stamps
			// y-axis: stacks of category ratings

			foreach (var category in review.Categories) {
				report.ReportData.Add(
					new ReportDataRow
						{
							Title = category.Name,
							Values = review.ReviewRoundsInOrder.Select(round => {
								var assessments = round.Feedback.SelectMany(feedback => feedback.Assessments)
									.Where(assess => assess.ReviewCategory == category)
									.Where(assess => assess.ReviewedPeer == peer)
									.Where(assess => !myRatingsOnly || assess.Reviewer == peer)
									.ToList();

								return Convert.ToDecimal(
									assessments.Any()
										? assessments.Average(ass => ass.Rating)
										: 0);
							}).ToList()
						});
			}

			AddSumsToReportData(review, report);

			return report;
		}

		public Report CreateTeamStackReport(int reviewId) {
			var review = RetrieveValidReviewOrFail(reviewId);
			var report = new Report
				             {
					             Title = "Stack Ratings for all peers over all categories",
					             Description = "A stack of peer ratings summated over all categories",
					             ReviewName = review.Name,
					             CategoryCount = review.Categories.Count,
					             XAxisLabels = review.ReviewRoundsInOrder.Select(round => round.StartDate.ToShortDateString()).ToList()
				             };

			// x-axis: time-stamps
			// y-axis: stacks of peer ratings summated over all categories

			foreach (var peer in review.Peers) {
				report.ReportData.Add(
					new ReportDataRow
						{
							Title = peer.UserName,
							Values = review.ReviewRoundsInOrder.Select(round => Convert.ToDecimal(
								round.Feedback.SelectMany(feedback => feedback.Assessments)
									.Where(assess => assess.ReviewedPeer == peer)
									.Where(assess => review.Categories.Contains(assess.ReviewCategory))
									.GroupBy(assess => assess.ReviewCategory)
									.Select(category => category.Average(cat => cat.Rating))
									.Sum())).ToList()
						});
			}

			AddSumsToReportData(review, report);

			return report;
		}

		private static void AddSumsToReportData(ReviewConfiguration review, Report report) {
			var stackRankingSums = new ReportDataRow {
				Title = "Total",
				Values = GetSums(review.ReviewRoundsInOrder.Count(), report.ReportData)
			};
			report.ReportData.Add(stackRankingSums);
		}

		private static IList<decimal> GetSums(int count, IList<ReportDataRow> reportData) {
			var sums = new List<decimal>();
			for (int i = 0; i < count; i++) {
				sums.Add(reportData.Sum(row => row.Values[i]));
			}
			return sums;
		}

		private static void VerifyIntegrityOfLoggedInUser(string loggedInUserEmail, bool myRatingsOnly, UserProfile peer) {
			if (myRatingsOnly && peer.EmailAddress != loggedInUserEmail) {
				throw new ArgumentException("There's a mismatch between the logged in user and the requested peer!",
				                            "loggedInUserEmail");
			}
		}

		private static ReviewCategory RetrieveValidCategoryOrFail(int categoryId, ReviewConfiguration review) {
			var category = review.Categories.SingleOrDefault(p => p.Id == categoryId);
			if (category == null) {
				throw new ArgumentNullException("categoryId", "No category with the given id exists in this review!");
			}
			return category;
		}

		private static UserProfile RetrieveValidPeerOrFail(int peerId, ReviewConfiguration review) {
			var peer = review.Peers.SingleOrDefault(p => p.Id == peerId);
			if (peer == null) {
				throw new ArgumentNullException("peerId", "No peer with the given id exists in this review!");
			}
			return peer;
		}

		private ReviewConfiguration RetrieveValidReviewOrFail(int reviewId) {
			var review = _db.ReviewConfigurations.Find(reviewId);
			if (review == null) {
				throw new ArgumentNullException("reviewId", "No review with the given id exists!");
			}
			return review;
		}

		private static ReportDataRow OwnRatingsOverTime(IEnumerable<ReviewRound> reviewRounds, UserProfile ratedUser,
		                                                ReviewCategory category) {
			var title = string.Format("{0} (by {0})", ratedUser.UserName);
			return CreateReportDataRow(reviewRounds, ratedUser, category, title, true);
		}

		private static ReportDataRow TeamRatingsOverTime(IEnumerable<ReviewRound> reviewRounds, UserProfile ratedUser,
											 ReviewCategory category) {
			var title = ratedUser != null ? ratedUser.UserName + " (by team)" : "team (by team)";
			return CreateReportDataRow(reviewRounds, ratedUser, category, title, false);
		}

		private static ReportDataRow CreateReportDataRow(IEnumerable<ReviewRound> reviewRounds, UserProfile ratedUser, 
			ReviewCategory category, string title, bool onlyOwnRatings) {
			return new ReportDataRow
				       {
					       Title = title,
					       Values = reviewRounds
						       .Select(round => {
							       var assessments = round.Feedback.SelectMany(feedback => feedback.Assessments)
								       .Where(ass => ass.ReviewCategory == category)
									   .Where(ass => onlyOwnRatings ? ass.ReviewedPeer == ratedUser : (ratedUser == null || ass.ReviewedPeer == ratedUser))
								       .Where(ass => !onlyOwnRatings || ass.Reviewer == ratedUser)
								       .ToList();

							       return Convert.ToDecimal(assessments.Any()
								                                ? assessments.Average(ass => ass.Rating)
								                                : 0);
						       }).ToList()
				       };
		}


	}
}