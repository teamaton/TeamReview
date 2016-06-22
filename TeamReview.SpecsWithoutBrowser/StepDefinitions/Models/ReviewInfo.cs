using TeamReview.Core.Models;
using TeamReview.Web.ViewModels;

namespace TeamReview.SpecsWithoutBrowser.StepDefinitions.Models {
	public class ReviewInfo {
		public ReviewConfiguration ReviewConfiguration;
		public ReviewCategory CurrentCategory;
		public string TypeOfReport { get; set; }
		public UserProfile CurrentUser { get; set; }
		public ReportViewModel ReportViewModel { get; set; }
	}
}
