using System.Linq;
using System.Web.Mvc;
using TeamReview.Core.DataAccess;

namespace TeamReview.Web.Filters {
	public class HttpNotFoundIfInvalidIdAttribute : ActionFilterAttribute {
		public override void OnActionExecuting(ActionExecutingContext filterContext) {
			object reviewIdObj;
			if (!filterContext.ActionParameters.TryGetValue("id", out reviewIdObj))
				return;

			var reviewId = ActionFilterExtensions.GetIdAsInt(reviewIdObj);
			var db = new DatabaseContext();

			if (reviewId == 0 || db.ReviewConfigurations.Count(r => r.Id == reviewId) < 1) {
				filterContext.Result = new HttpNotFoundResult("No review with the given id can be found.");
			}
			else {
				base.OnActionExecuting(filterContext);
			}
		}
	}
}