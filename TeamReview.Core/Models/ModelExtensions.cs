using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamReview.Core.Models {
	public static class ModelExtensions {
		public static decimal GetOwnRatingForCategory(
			this IEnumerable<ReviewFeedback> reviewFeedbacks, int userId, ReviewCategory category) {
			if (reviewFeedbacks == null) throw new ArgumentNullException("reviewFeedbacks");

			var assessment = reviewFeedbacks.SelectMany(fb => fb.Assessments)
				.Where(a => a.ReviewCategory.Id == category.Id)
				.SingleOrDefault(a => a.ReviewedPeer.Id == userId && a.Reviewer.Id == userId);

			return assessment != null ? assessment.Rating : 0;
		}

		public static decimal GetPeerRatingForPeerForCategory(
			this IEnumerable<ReviewFeedback> reviewFeedbacks, int peerId, ReviewCategory category) {
			if (reviewFeedbacks == null) throw new ArgumentNullException("reviewFeedbacks");

			var otherReviewersCount = reviewFeedbacks.Count(fb => fb.Reviewer.Id != peerId);
			return otherReviewersCount > 0
				       ? reviewFeedbacks.SelectMany(fb => fb.Assessments)
					         .Where(a => a.ReviewCategory.Id == category.Id)
					         .Where(a => a.ReviewedPeer.Id == peerId && a.Reviewer.Id != peerId)
					         .Sum(a => a.Rating)/(decimal) otherReviewersCount
				       : 0;
		}

		public static IEnumerable<string> Names(this IEnumerable<UserProfile> users) {
			if (users == null) return new string[0];
			return users.Select(u => u.UserName);
		}
	}
}