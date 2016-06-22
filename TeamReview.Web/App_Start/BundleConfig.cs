using System.Web.Optimization;

namespace TeamReview.Web {
	public class BundleConfig {
		// For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles) {
			// http://stackoverflow.com/questions/13246327/asp-net-scriptbundle-does-not-work-with-min-js
			bundles.IgnoreList.Clear();

			bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
				"~/Scripts/jquery-{version}.js"));

			bundles.Add(new ScriptBundle("~/bundles/jquery-stuff").Include(
				"~/Scripts/jquery.toc.js"));

			bundles.Add(new ScriptBundle("~/bundles/editinplace").Include(
				"~/Scripts/jquery.edit-in-place.js"));

			bundles.Add(new ScriptBundle("~/bundles/jqplot").Include(
				"~/Scripts/jquery.jqplot.js",
				"~/Scripts/jqplot.pointLabels.js",
				"~/Scripts/jqplot.barRenderer.js",
				"~/Scripts/jqplot.categoryAxisRenderer.js",
				"~/Scripts/jqplot.tn.reports.js"));

			bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
				"~/Scripts/jquery.unobtrusive-ajax.js",
				"~/Scripts/jquery.validate.js"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
				"~/Scripts/modernizr-*"));

			bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
				"~/Scripts/bootstrap.js"));

			bundles.Add(new StyleBundle("~/Content/css").Include(
				"~/Content/layout.css"));

			bundles.Add(new StyleBundle("~/Content/jqplotcss").Include(
				"~/Content/jquery.jqplot.css"));

		}
	}
}