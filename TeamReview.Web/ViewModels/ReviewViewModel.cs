namespace TeamReview.Web.ViewModels {
	public class ReviewViewModel {
		public int ReviewId { get; set; }
		public string Name { get; set; }
		public ActionStatus ActionStatus { get; set; }
	}

	public enum ActionStatus {
		NotStarted,
		ProvideReview,
		WaitForReviews,
		ShowResults
	}
}