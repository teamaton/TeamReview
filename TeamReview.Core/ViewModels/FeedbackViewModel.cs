using System.Collections.Generic;
using System.Linq;

namespace TeamReview.Core.ViewModels {
	public class FeedbackViewModel {
		public FeedbackViewModel() {
			CategoriesWithPeersAndRatings = new List<CategoryWithPeersAndRatings>();
		}

		public int ReviewId { get; set; }
		public string ReviewName { get; set; }

		public IList<CategoryWithPeersAndRatings> CategoriesWithPeersAndRatings { get; set; }

		public bool IsIncomplete {
			get {
				return CategoriesWithPeersAndRatings
					.SelectMany(c => c.PeersWithRatings.Select(p => p.Rating))
					.Any(a => a < 1 || a > 10);
			}
		}
	}
}