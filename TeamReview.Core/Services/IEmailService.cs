namespace TeamReview.Core.Services {
	public interface IEmailService {
		void SendInvitationEmailsForReview(int reviewConfigurationId);
	}
}