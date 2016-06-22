using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TeamReview.Web.ViewModels;

namespace TeamReview.Web {
	public class MvcApplication : HttpApplication {
		protected void Application_Start() {
			AreaRegistration.RegisterAllAreas();
			AutofacConfig.RegisterDependencies();
			WebApiConfig.Register(GlobalConfiguration.Configuration);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
			AuthConfig.RegisterAuth();
			AutoMapperBootstrap.Initialize();
		}
	}
}