using System;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Routing;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;

namespace TeamReview.Web.Filters {
	public class DenyDuplicateFeedbackAttribute : ActionFilterAttribute {
		public override void OnActionExecuting(ActionExecutingContext filterContext) {
			var reviewId = this.GetIdValue(filterContext);
			var db = new DatabaseContext();
			var currentUserName = filterContext.HttpContext.User.Identity.Name;

			Expression<Func<ReviewConfiguration, bool>> userHasAlreadyProvidedFeedback =
				r =>
				r.Id == reviewId &&
				r.ReviewRounds.FirstOrDefault(round => round.Active) != null &&
				// must use FirstOrDefault, First does not work with EF
				r.ReviewRounds.FirstOrDefault(round => round.Active).Feedback.Any(fb => fb.Reviewer.EmailAddress == currentUserName);

			var review = db.ReviewConfigurations.Where(userHasAlreadyProvidedFeedback).SingleOrDefault();
			if (review != null) {
				filterContext.Controller.TempData["Message"] =
					string.Format("You have already completed the review '{0}'. Thank you!", review.Name);
				filterContext.Result =
					new RedirectToRouteResult(new RouteValueDictionary(new { action = "Index", controller = "Review" }));
			}

			base.OnActionExecuting(filterContext);
		}
	}
}