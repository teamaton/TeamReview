using System.Collections.Generic;
using TeamReview.Core.Models;

namespace TeamReview.Core.Services {
	public interface IReportService {
		Report CreateOwnReport(int reviewId, string loggedInUserEmail, int categoryId);
		Report CreateOwnStackReport(int reviewId, string loggedInUserEmail);
		Report CreatePeerReport(int reviewId, IList<int> selectedUserIds, int categoryId);
		Report CreateTeamReport(int reviewId, int categoryId);
		Report CreateTeamStackReport(int reviewId);
	}
}