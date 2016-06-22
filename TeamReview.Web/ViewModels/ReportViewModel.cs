namespace TeamReview.Web.ViewModels {
	public class ReportViewModel {
		public ReportViewModel() {
			// initialize to empty arrays
			XAxisLabels = DataRows = DataRowsLabels = "[]";
		}
		public string ReviewName { get; set; }
		public int CategoryCount { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public string XAxisLabels { get; set; }
		public string DataRows { get; set; }
		public string DataRowsLabels { get; set; }

		// used for id of chart div and mapping of javascript
		public string FieldId { get; set; }
	}
}
