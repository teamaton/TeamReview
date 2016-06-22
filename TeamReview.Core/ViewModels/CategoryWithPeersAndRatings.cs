using System.Collections.Generic;

namespace TeamReview.Core.ViewModels {
	public class CategoryWithPeersAndRatings {
		public CategoryWithPeersAndRatings() {
			PeersWithRatings = new List<PeerWithRating>();
		}

		public CategoryShowModel Category { get; set; }
		public IList<PeerWithRating> PeersWithRatings { get; set; }
	}
}