namespace TeamReview.Core.Services {
	public interface IReviewService {
		void StartReview(int reviewConfigurationId, string email);

		void StartNewReviewRounds();
	}
}