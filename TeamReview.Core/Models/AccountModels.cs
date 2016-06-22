using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.DynamicData;
using System.Web.Mvc;
using DataAnnotationsExtensions;

namespace TeamReview.Core.Models {
	[TableName("UserProfile")]
	public class UserProfile {
		public int Id { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
		public ICollection<ReviewConfiguration> ReviewConfigurations { get; set; }
	}

	public class RegisterExternalLoginModel {
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		[Required]
		[Email]
		[Display(Name = "Email address")]
		public string EmailAddress { get; set; }

		public string ExternalLoginData { get; set; }
	}

	public class LocalPasswordModel {
		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Current password")]
		public string OldPassword { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "New password")]
		public string NewPassword { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm new password")]
		[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}

	public class LoginModel {
		[Required]
		[Display(Name = "Email Address")]
		public string EmailAddress { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[Display(Name = "Remember me?")]
		public bool RememberMe { get; set; }
	}

	public class RegisterModel {
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm password")]
		[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }

		public string EmailAddress { get; set; }
	}

	public class ExternalLogin {
		public string Provider { get; set; }
		public string ProviderDisplayName { get; set; }
		public string ProviderUserId { get; set; }
	}
}