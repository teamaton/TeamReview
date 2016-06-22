using System;
using TeamReview.Core.DataAccess;
using TeamReview.Web.Controllers;

namespace TeamReview.SpecsWithoutBrowser.StepDefinitions.Models {
	public class CommonContext {
		public Lazy<ReportController> ReportController { get; set; }
		public Lazy<FeedbackController> FeedbackController { get; set; }
		public Lazy<ReviewController> ReviewController { get; set; }
		public DatabaseContext DatabaseContext { get; set; }
	}
}
