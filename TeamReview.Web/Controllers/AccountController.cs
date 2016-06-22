using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Transactions;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.Web.WebPages.OAuth;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;
using TeamReview.Core.Services;
using TeamReview.Web.Filters;
using WebMatrix.WebData;

namespace TeamReview.Web.Controllers {
	[Authorize]
	[InitializeSimpleMembership]
	public class AccountController : Controller {
		private const string PasswordPlaceholder = "passwordPlaceholder";
		//
		// GET: /Account/Login

		[AllowAnonymous]
		public ActionResult Login(string returnUrl) {
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}

		//
		// POST: /Account/Login

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Login(LoginModel model, string returnUrl) {
			if (ModelState.IsValid && WebSecurity.Login(model.EmailAddress, model.Password, persistCookie: model.RememberMe)) {
				return RedirectToLocal(returnUrl);
			}

			// If we got this far, something failed, redisplay form
			ModelState.AddModelError("", "The user name or password provided is incorrect.");
			return View(model);
		}

		//
		// POST: /Account/LogOff

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LogOff() {
			WebSecurity.Logout();

			return RedirectToAction("Login", "Account");
		}

		//
		// GET: /Account/Register

		[AllowAnonymous]
		public ActionResult Register() {
			return View();
		}

		//
		// POST: /Account/Register

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Register(string emailAddress) {
			if (ModelState.IsValid) {
				// Attempt to register the user
				try {
					string confirmationToken;
					if (WebSecurity.UserExists(emailAddress)) {
						confirmationToken = WebSecurity.CreateAccount(emailAddress, PasswordPlaceholder, true);
					}
					else {
						confirmationToken = WebSecurity.CreateUserAndAccount(emailAddress, PasswordPlaceholder,
						                                                     new { UserName = "not_set" }, true);
					}

					var message = new MailMessage(EmailService.DefaultContactEmail, emailAddress)
						              {
							              Subject = "Confirm Registration",
							              Body = GetMailBody(confirmationToken, emailAddress)
						              };
					new SmtpClient().Send(message);
					TempData["Message"] = "An email has been send to " + emailAddress +
					                      ". Please check your inbox for further instructions.";
					ModelState.Clear();
					return View();
				}
				catch (MembershipCreateUserException e) {
					TempData["Message"] = e.Message;
				}
			}

			// If we got this far, something failed, redisplay form
			return View();
		}

		private string GetMailBody(string confirmationToken, string emailAddress) {
			return string.Format(
				@"Hi there,

you have completed the first step for the registration to TeamReview.

Please follow this link to complete your registration: {0}

If you would like to find our more about TeamReview, feel free to visit <a href='http://www.teamreview.net'>TeamReview.net</a>

If you did not register with your email address for TeamReview, we are sorry that somebody misused your email address.

Thank you for your time and cheers,

Andrej - Masterchief Head of Design of TeamReview.net

",
				"http://www.teamreview.net/Account/CompleteRegistration?confirmationToken=" + confirmationToken + "&email=" +
				emailAddress);
		}

		[AllowAnonymous]
		public ActionResult CompleteRegistration(string confirmationToken, string email) {
			if (WebSecurity.ConfirmAccount(confirmationToken)) {
				return View(new RegisterModel() { EmailAddress = email });
			}
			return RedirectToAction("Index", "Home");
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult CompleteRegistration(RegisterModel model) {
			if (ModelState.IsValid) {
				try {
					WebSecurity.ChangePassword(model.EmailAddress, PasswordPlaceholder, model.Password);
					var db = new DatabaseContext();
					var user = db.UserProfiles.Find(WebSecurity.GetUserId(model.EmailAddress));
					user.UserName = model.UserName;
					db.SaveChanges();
					WebSecurity.Login(model.EmailAddress, model.Password);
					return RedirectToAction("Index", "Review");
				}
				catch (MembershipCreateUserException e) {
					ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// POST: /Account/Disassociate

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Disassociate(string provider, string providerUserId) {
			var ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
			ManageMessageId? message = null;

			// Only disassociate the account if the currently logged in user is the owner
			if (ownerAccount == User.Identity.Name) {
				// Use a transaction to prevent the user from deleting their last login credential
				using (
					var scope = new TransactionScope(TransactionScopeOption.Required,
					                                 new TransactionOptions { IsolationLevel = IsolationLevel.Serializable })) {
					var hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
					if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1) {
						OAuthWebSecurity.DeleteAccount(provider, providerUserId);
						scope.Complete();
						message = ManageMessageId.RemoveLoginSuccess;
					}
				}
			}

			return RedirectToAction("Manage", new { Message = message });
		}

		//
		// GET: /Account/Manage

		public ActionResult Manage(ManageMessageId? message) {
			ViewBag.StatusMessage =
				message == ManageMessageId.ChangePasswordSuccess
					? "Your password has been changed."
					: message == ManageMessageId.SetPasswordSuccess
						  ? "Your password has been set."
						  : message == ManageMessageId.RemoveLoginSuccess
							    ? "The external login was removed."
							    : "";
			ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			ViewBag.ReturnUrl = Url.Action("Manage");
			return View();
		}

		//
		// POST: /Account/Manage

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Manage(LocalPasswordModel model) {
			var hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			ViewBag.HasLocalPassword = hasLocalAccount;
			ViewBag.ReturnUrl = Url.Action("Manage");
			if (hasLocalAccount) {
				if (ModelState.IsValid) {
					// ChangePassword will throw an exception rather than return false in certain failure scenarios.
					bool changePasswordSucceeded;
					try {
						changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
					}
					catch (Exception) {
						changePasswordSucceeded = false;
					}

					if (changePasswordSucceeded) {
						return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
					}
					else {
						ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
					}
				}
			}
			else {
				// User does not have a local password so remove any validation errors caused by a missing
				// OldPassword field
				var state = ModelState["OldPassword"];
				if (state != null) {
					state.Errors.Clear();
				}

				if (ModelState.IsValid) {
					try {
						WebSecurity.CreateAccount(User.Identity.Name, model.NewPassword);
						return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
					}
					catch (Exception e) {
						ModelState.AddModelError("", e);
					}
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// POST: /Account/ExternalLogin

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLogin(string provider, string returnUrl) {
			return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
		}

		//
		// GET: /Account/ExternalLoginCallback

		[AllowAnonymous]
		public ActionResult ExternalLoginCallback(string returnUrl) {
			var result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
			if (!result.IsSuccessful) {
				return RedirectToAction("ExternalLoginFailure");
			}

			if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false)) {
				return RedirectToLocal(returnUrl);
			}

			if (User.Identity.IsAuthenticated) {
				// If the current user is logged in add the new account
				OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
				return RedirectToLocal(returnUrl);
			}
			else {
				// User is new, ask for their desired membership name and contact email address
				string email;
				result.ExtraData.TryGetValue("email", out email);
				var userName = result.UserName;
				var indexOfAt = userName.IndexOf('@');
				if (indexOfAt > 0) userName = userName.Substring(0, indexOfAt);
				var loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
				ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
				ViewBag.ReturnUrl = returnUrl;
				return View("ExternalLoginConfirmation",
				            new RegisterExternalLoginModel
					            {
						            EmailAddress = email,
						            UserName = userName,
						            ExternalLoginData = loginData
					            });
			}
		}

		//
		// POST: /Account/ExternalLoginConfirmation

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl) {
			string provider;
			string providerUserId;

			if (User.Identity.IsAuthenticated ||
			    !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId)) {
				return RedirectToAction("Manage");
			}

			if (ModelState.IsValid) {
				// Insert a new user into the database
				using (var db = new DatabaseContext()) {
					// Check if user already exists
					var user = db.UserProfiles.FirstOrDefault(u => u.EmailAddress.ToLower() == model.EmailAddress.ToLower());
					if (user != null) {
						// Update UserName
						user.UserName = model.UserName;
					}
					else {
						// Insert new user into the profile table
						db.UserProfiles.Add(new UserProfile { UserName = model.UserName, EmailAddress = model.EmailAddress });
					}
					db.SaveChanges();

					OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.EmailAddress);
					OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

					return RedirectToLocal(returnUrl);
				}
			}

			ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
			ViewBag.ReturnUrl = returnUrl;
			return View(model);
		}

		//
		// GET: /Account/ExternalLoginFailure

		[AllowAnonymous]
		public ActionResult ExternalLoginFailure() {
			return View();
		}

		[AllowAnonymous]
		[ChildActionOnly]
		public ActionResult ExternalLoginsList(string returnUrl) {
			ViewBag.ReturnUrl = returnUrl;
			return PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
		}

		[ChildActionOnly]
		public ActionResult RemoveExternalLogins() {
			var accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name);
			var externalLogins = new List<ExternalLogin>();
			foreach (var account in accounts) {
				var clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

				externalLogins.Add(new ExternalLogin
					                   {
						                   Provider = account.Provider,
						                   ProviderDisplayName = clientData.DisplayName,
						                   ProviderUserId = account.ProviderUserId,
					                   });
			}

			ViewBag.ShowRemoveButton = externalLogins.Count > 1 ||
			                           OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			return PartialView("_RemoveExternalLoginsPartial", externalLogins);
		}

		#region Helpers

		public enum ManageMessageId {
			ChangePasswordSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
		}

		private ActionResult RedirectToLocal(string returnUrl) {
			if (Url.IsLocalUrl(returnUrl)) {
				return Redirect(returnUrl);
			}
			else {
				return RedirectToAction("Index", "Review");
			}
		}

		private static string ErrorCodeToString(MembershipCreateStatus createStatus) {
			// See http://go.microsoft.com/fwlink/?LinkID=177550 for
			// a full list of status codes.
			switch (createStatus) {
				case MembershipCreateStatus.DuplicateUserName:
					return "User name already exists. Please enter a different user name.";

				case MembershipCreateStatus.DuplicateEmail:
					return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

				case MembershipCreateStatus.InvalidPassword:
					return "The password provided is invalid. Please enter a valid password value.";

				case MembershipCreateStatus.InvalidEmail:
					return "The e-mail address provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidAnswer:
					return "The password retrieval answer provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidQuestion:
					return "The password retrieval question provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidUserName:
					return "The user name provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.ProviderError:
					return
						"The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				case MembershipCreateStatus.UserRejected:
					return
						"The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				default:
					return
						"An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
			}
		}

		internal class ExternalLoginResult : ActionResult {
			public ExternalLoginResult(string provider, string returnUrl) {
				Provider = provider;
				ReturnUrl = returnUrl;
			}

			public string Provider { get; private set; }
			public string ReturnUrl { get; private set; }

			public override void ExecuteResult(ControllerContext context) {
				OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
			}
		}

		#endregion
	}
}