using System;
using System.Net.Mail;
using System.Web.Mvc;
using TeamReview.Core.Services;
using TeamReview.Web.ViewModels;

namespace TeamReview.Web.Controllers {
	public class HomeController : Controller {
        private readonly ISmtpClient _smtpClient;

        public HomeController(ISmtpClient smtpClient)
	    {
	        _smtpClient = smtpClient;
	    }

	    //
		// GET: /Home/

		public ActionResult Index() {
			return View();
		}

		public ActionResult Features() {
			return View();
		}

		public ActionResult StackRanking() {
			return View();
		}

		public ActionResult PeerReviews() {
			return View();
		}

		public ActionResult PerformanceReview() {
			return View();
		}

		public ActionResult VisualizeData() {
			return View();
		}

		public ActionResult AboutUs() {
			return View();
		}

		public ActionResult Contact() {
			return View();
		}

		[HttpPost]
		public ActionResult Contact(ContactViewModel userdata) {
			if (!ModelState.IsValid) {
				return View();
			}

			var messageBody = string.Format("{0}{1}---{1}Sent by: {2}", userdata.Message,
			                                Environment.NewLine, userdata.GetNameDetails());
			var message = new MailMessage(EmailService.DefaultContactEmail, EmailService.DefaultContactEmail)
				              {
					              Body = messageBody,
					              Subject = "TeamReview.net - contact form"
				              };
			var displayName = userdata.GetDisplayName();
			message.ReplyToList.Add(new MailAddress(userdata.EmailAddress, displayName));

			_smtpClient.Create().Send(message);

			TempData["MessageSent"] = "true";
			return RedirectToAction("Contact");
		}
	}
}