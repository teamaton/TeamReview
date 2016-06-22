using System.Linq;
using System.Web.Mvc;
using TeamReview.Core.DataAccess;

namespace TeamReview.Web.Filters {
	public class UserNameFilter : IActionFilter {
		public void OnActionExecuting(ActionExecutingContext filterContext) {
			SaveCurrentUserNameInViewBag(filterContext);
		}

		public void OnActionExecuted(ActionExecutedContext filterContext) {
		}

		private static void SaveCurrentUserNameInViewBag(ActionExecutingContext filterContext) {
			var user = filterContext.HttpContext.User.Identity;
			if (user.IsAuthenticated) {
				var dbContext = new DatabaseContext();
				var userProfile = dbContext.UserProfiles.FirstOrDefault(u => u.EmailAddress == user.Name);
				if (userProfile != null) {
					filterContext.Controller.ViewBag.UserName = userProfile.UserName;
				}
			}
		}
	}
}