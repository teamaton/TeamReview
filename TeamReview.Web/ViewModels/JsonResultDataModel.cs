namespace TeamReview.Web.ViewModels {
	public class JsonResultDataModel {
		// ReSharper disable InconsistentNaming
		public string status { get; private set; }
		public string message { get; private set; }
		// ReSharper restore InconsistentNaming

		public JsonResultDataModel Status(string value) {
			status = value;
			return this;
		}

		public JsonResultDataModel Message(string value) {
			message = value;
			return this;
		}
	}
}