using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;

namespace TeamReview.Web.Controllers {
	public class LogOnController : Controller {
		//
		// GET: /LogOn/

		public ActionResult LogOn() {
			var openid = new OpenIdRelyingParty();
			var response = openid.GetResponse();

			if (response != null) {
				switch (response.Status) {
					case AuthenticationStatus.Authenticated:
						var fetch = response.GetExtension<FetchResponse>();
						if (fetch != null) {
							foreach (var attribute in fetch.Attributes) {
								ViewData.Add(attribute.TypeUri, string.Join("|", attribute.Values));
							}
						}
						//response.ClaimedIdentifier
						//return RedirectToAction("LogOn");
						break;
					case AuthenticationStatus.Canceled:
						ModelState.AddModelError("loginIdentifier",
						                         "Login was cancelled at the provider");
						break;
					case AuthenticationStatus.Failed:
						ModelState.AddModelError("loginIdentifier",
						                         "Login failed using the provided OpenID identifier");
						break;
				}
			}

			return View();
		}

		[HttpPost]
		public ActionResult LogOn(string loginIdentifier) {
			if (!Identifier.IsValid(loginIdentifier)) {
				ModelState.AddModelError("loginIdentifier",
				                         "The specified login identifier is invalid");
				return View();
			}

			var openid = new OpenIdRelyingParty();
			var request = openid.CreateRequest(Identifier.Parse(loginIdentifier));

			// Require some additional data
			request.AddExtension(new ClaimsRequest
				                     {
					                     BirthDate = DemandLevel.NoRequest,
					                     Email = DemandLevel.Require,
					                     FullName = DemandLevel.Require,
					                     Nickname = DemandLevel.Request,
				                     });

			return request.RedirectingResponse.AsActionResult();
		}
	}
}