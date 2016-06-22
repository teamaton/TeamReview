using System.Collections.Generic;
using TeamReview.Core.Models;

namespace TeamReview.Web.ViewModels {
	public class ResultViewModel {
		public int ReviewId { get; set; }

		public string ReviewName { get; set; }

		public int LoggedInUserId { get; set; }

		public IEnumerable<UserProfile> Peers { get; set; }

		public IEnumerable<UserProfile> Reviewers { get; set; }

		public string CategoriesJson { get; set; }

		public string PeersJson { get; set; }

		public string MyResultsJson { get; set; }

		public string PeerRatingsPerCategoryJson { get; set; }

		public string CategoryResultsPerPeerJson { get; set; }

		public string StackRankingSumLabels { get; set; }
	}
}