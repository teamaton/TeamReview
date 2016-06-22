using System.Collections.Generic;

namespace TeamReview.Core.Models {
	public class Report {
		public string Title { get; set; }
		public string Description { get; set; }
		public string ReviewName { get; set; }
		public int CategoryCount { get; set; }
		public IList<ReportDataRow> ReportData { get; set; }
		public IList<string> XAxisLabels { get; set; }

		public Report() {
			ReportData = new List<ReportDataRow>();
		}
	}

	public class ReportDataRow {
		public ReportDataRow() {
			Values = new List<decimal>();
		}

		public string Title { get; set; }
		public IList<decimal> Values { get; set; }
	}
}