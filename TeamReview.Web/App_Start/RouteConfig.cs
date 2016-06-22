using System.Web.Mvc;
using System.Web.Routing;

namespace TeamReview.Web {
	public class RouteConfig {
		public static void RegisterRoutes(RouteCollection routes) {
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				name: null,
				url: "dashboard",
				defaults: new { controller = "Review", action = "Index" }
				);

			routes.MapRoute(
				name: null,
				url: "stack-ranking",
				defaults: new { controller = "Home", action = "StackRanking" }
				);

			routes.MapRoute(
				name: null,
				url: "peer-reviews",
				defaults: new { controller = "Home", action = "PeerReviews" }
				);

			routes.MapRoute(
				name: null,
				url: "performance-review",
				defaults: new { controller = "Home", action = "PerformanceReview" }
				);

			routes.MapRoute(
				name: null,
				url: "visualize-data",
				defaults: new { controller = "Home", action = "VisualizeData" }
				);

			routes.MapRoute(
				name: null,
				url: "Reports/{id}",
				defaults: new { controller = "Report", action = "Index", id = UrlParameter.Optional }
				);

			routes.MapRoute(
				name: "Default",
				url: "{controller}/{action}/{id}",
				defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
				);
		}
	}
}