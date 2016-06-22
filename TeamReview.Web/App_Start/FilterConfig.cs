using System.Web.Mvc;
using TeamReview.Web.Filters;

namespace TeamReview.Web {
	public class FilterConfig {
		public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
			filters.Add(new HandleErrorAttribute());
			filters.Add(new UserNameFilter());
		}
	}
}