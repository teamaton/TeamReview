using TeamReview.Core.ViewModels;

namespace TeamReview.Core.Services {
	public interface IFeedbackService {
		void SaveFeedback(string userEmail, FeedbackViewModel viewModel);
	}
}