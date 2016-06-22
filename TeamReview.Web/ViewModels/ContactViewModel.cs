using System.ComponentModel.DataAnnotations;
using DataAnnotationsExtensions;

namespace TeamReview.Web.ViewModels {
	public class ContactViewModel {
		[Required]
		[Email]
		public string EmailAddress { get; set; }

		public string FirstName { get; set; }
		public string LastName { get; set; }

		[Required]
		public string Message { get; set; }

		public bool MessageSuccessfullySent { get; set; }

		internal string GetDisplayName() {
			var name = LastName;
			if (string.IsNullOrWhiteSpace(name)) {
				name = FirstName;
			}
			else if (!string.IsNullOrWhiteSpace(FirstName)) {
				name += ", " + FirstName;
			}
			name = string.IsNullOrWhiteSpace(name) ? null : name;
			return name;
		}

		internal string GetNameDetails() {
			return
				(string.IsNullOrWhiteSpace(LastName) ? "[no last name]" : LastName.Trim())
				+ ", " +
				(string.IsNullOrWhiteSpace(FirstName) ? "[no first name]" : FirstName.Trim());
		}
	}
}