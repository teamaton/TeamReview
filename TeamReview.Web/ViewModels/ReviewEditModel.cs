using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TeamReview.Core;
using TeamReview.Core.ViewModels;

namespace TeamReview.Web.ViewModels {
	public class ReviewCreateEditModel {
		public ReviewCreateEditModel() {
			LengthOfReviewRoundInWeeks = 2;
			ExistingCategories = new CategoryShowModel[0];
			ExistingPeers = new PeerShowModel[0];
			AddedCategories = new List<CategoryAddModel>();
			AddedPeers = new List<PeerAddModel>();
		}

		public int Id { get; set; }

		public string Name { get; set; }

		[DisplayName("Time between review rounds in weeks")]
		public int LengthOfReviewRoundInWeeks { get; set; }

		public CategoryShowModel[] ExistingCategories { get; set; }

		public PeerShowModel[] ExistingPeers { get; set; }

		public IList<CategoryAddModel> AddedCategories { get; set; }

		public IList<PeerAddModel> AddedPeers { get; set; }

		public bool NoCategoriesAddedYet {
			get { return !ExistingCategories.Any() && !AddedCategories.Any(); }
		}

		public bool NoPeersInvitedYet {
			get { return ExistingPeers.Count() <= 1 && !AddedPeers.Any(); }
		}

		public bool Active { get; set; }
	}
}