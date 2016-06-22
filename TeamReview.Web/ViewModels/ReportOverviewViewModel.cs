using System.Collections.Generic;
using TeamReview.Core.Models;

namespace TeamReview.Web.ViewModels {
	public class ReportOverviewViewModel {
		public int ReviewId { get; set; }
		public string Title { get; set; }
		public IList<int> PeerIds { get; set; }
		public int LoggedInUserId { get; set; }
		public IList<int> CategoryIds { get; set; }

		public IList<ReviewCategory> Categories { get; set; }
		public int CategoryId { get; set; }

		public IList<UserProfile> Peers { get; set; }
		public IList<int> SelectedPeerIds { get; set; }

		public bool OneTeamReportPerCategory { get; set; }
	}
}