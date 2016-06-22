using System.Collections.Generic;

namespace TeamReview.Web.ViewModels
{
	public class CategoriesReportViewModel
	{
		public CategoriesReportViewModel()
		{
			ReportModels=new List<ReportViewModel>();
		}

		public string ReviewName { get; set; }
		public string Title { get; set; }
		public IList<ReportViewModel> ReportModels { get; set; }
	}
}