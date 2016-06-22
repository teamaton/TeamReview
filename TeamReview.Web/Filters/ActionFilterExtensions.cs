using System;
using System.Web.Mvc;

namespace TeamReview.Web.Filters {
	internal static class ActionFilterExtensions {
		/// <summary>
		/// Tries to retrieve the id parameter from the currently executing action method.
		/// <br/>
		/// Throws ArgumentNullException if 'id' could not be found.
		/// <br/>
		/// Throws ArgumentException if 'id' is not an int.
		/// </summary>
		/// <returns>The int value of the 'id' parameter of the currently executing action method.</returns>
		internal static int GetIdValue(this ActionFilterAttribute actionFilter, ActionExecutingContext filterContext) {
			object reviewIdObj;
			if (!filterContext.ActionParameters.TryGetValue("id", out reviewIdObj)) {
				throw new ArgumentNullException(
					"id", string.Format(
						"Could not find 'id' parameter in action method '{0}' but need it to control access!",
						filterContext.ActionDescriptor.ActionName));
			}

			var reviewId = GetIdAsInt(reviewIdObj);
			return reviewId;
		}

		internal static int GetIdAsInt(object reviewIdObj) {
			int reviewId;
			try {
				reviewId = Convert.ToInt32(reviewIdObj);
			}
			catch (SystemException se) {
				throw new ArgumentException(
					string.Format("The 'id' parameter must be of type 'int' but was of type '{0}'!", reviewIdObj.GetType().Name),
					"id", se);
			}
			return reviewId;
		}
	}
}