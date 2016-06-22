using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Newtonsoft.Json;
using TeamReview.Core.DataAccess;
using TeamReview.Core.Models;
using TeamReview.Core.Services;
using TeamReview.Web.Filters;
using TeamReview.Web.ViewModels;

namespace TeamReview.Web.Controllers {
	[Authorize, AccessControl]
	public class ReportController : Controller {
		private readonly IReportService _reportService;
		private readonly IDatabaseContext _databaseContext;

		public ReportController(IDatabaseContext databaseContext, IReportService reportService) {
			_databaseContext = databaseContext;
			_reportService = reportService;
		}

		[HttpGet]
		public ActionResult Index(int id /* reviewId */) {
			var review = _databaseContext.ReviewConfigurations.Find(id);
			var reviewCategories = review.Categories.OrderBy(c => c.Name).ToList();
			var result = new ReportOverviewViewModel
				             {
					             ReviewId = id,
					             Title = string.Format(@"Reports for review ""{0}""", review.Name),
					             CategoryIds = review.Categories.Select(p => p.Id).ToList(),
								 Categories = reviewCategories,
								 CategoryId = 0, //reviewCategories.First().Id,
								 Peers = review.Peers,
					             PeerIds = review.Peers.Select(p => p.Id).ToList(),
					             LoggedInUserId = _databaseContext.UserProfiles.Single(user => user.EmailAddress == User.Identity.Name).Id,
								 SelectedPeerIds = new List<int>()
				             };

			return View(result);
		}

		[HttpPost]
		public ActionResult OwnReport(int id /*review.Id*/, int? categoryId) {
			if (categoryId == null) {
				return new EmptyResult();
			}

			var currentUserEmail = User.Identity.Name;
			var report = _reportService.CreateOwnReport(id, currentUserEmail, categoryId.Value);
			var viewModel = GetSingleReportViewModel(report);

			return PartialView("SingleReportPartial", viewModel);
		}

		[HttpGet]
		public ActionResult OwnStackRating(int id /* Review.Id */) {
			var report = _reportService.CreateOwnStackReport(id, User.Identity.Name);
			var viewModel = GetStackRatingViewModel(report, "cats");
			
			return PartialView("SingleReportStackRankingPartial", viewModel);
		}

		[HttpPost]
		public ActionResult PeerReport(int id /*review.Id*/, List<int> selectedPeerIds, int? categoryId) {
			if (categoryId == null) {
				return new EmptyResult();
			}

			var report = _reportService.CreatePeerReport(id, selectedPeerIds, categoryId.Value);
			var viewModel = GetSingleReportViewModel(report);

			return PartialView("SingleReportPartial", viewModel);
		}

		[HttpPost]
		public ActionResult TeamReport(int id /*review.Id*/, int? categoryId) {
			if (categoryId == null) {
				return new EmptyResult();
			}

			var report = _reportService.CreateTeamReport(id, categoryId.Value);
			var viewModel = GetSingleReportViewModel(report);
			return PartialView("SingleReportPartial", viewModel);
		}

		[HttpGet]
		public ActionResult TeamStackRating(int id /* Review.Id */) {
			var report = _reportService.CreateTeamStackReport(id);
			var viewModel = GetStackRatingViewModel(report, "sum");

			// adapt the category count to account for all categories per peer
			var review = _databaseContext.ReviewConfigurations.Find(id);
			viewModel.CategoryCount = review.Categories.Count*review.Peers.Count;

			return PartialView("SingleReportStackRankingPartial", viewModel);
		}

		private static ReportViewModel GetSingleReportViewModel(Report report) {
			var graphModel = Mapper.Map<ReportViewModel>(report);
			graphModel.XAxisLabels = JsonConvert.SerializeObject(report.XAxisLabels);
			graphModel.DataRows =
				JsonConvert.SerializeObject(report.ReportData.Select(data => data.Values));
			graphModel.DataRowsLabels =
				JsonConvert.SerializeObject(report.ReportData.Select(data => new { label = data.Title }));
			// viewModel.ReportModels.Add(graphModel);
			graphModel.FieldId = string.Format("results-chart-" + DateTime.UtcNow.Ticks);
			return  graphModel;
		}

		private static ReportViewModel GetStackRatingViewModel(Report report, string prefix) {
			var viewModel = Mapper.Map<ReportViewModel>(report);
			viewModel.FieldId = prefix + "-stack-rating-" + DateTime.Now.Ticks;
			viewModel.XAxisLabels = JsonConvert.SerializeObject(report.XAxisLabels);

			// label handling
			var lastReportDataRow = report.ReportData.Last();
			var allButLastReportDataRows = report.ReportData.Take(report.ReportData.Count - 1).ToList();
			var labels = allButLastReportDataRows.Select(data => new { label = data.Title }).ToList<object>();
			var sumLabels = lastReportDataRow.Values.Select(v => string.Format("∑ {0:#.##}", v));

			// add a last row of labels that display the sum of all values in each stack
			labels.Add(new
				           {
					           label = lastReportDataRow.Title,
					           color = "#fafafa" /* same as grid background color */,
					           pointLabels = new { labels = sumLabels }
				           });

			// finally, convert the labels to JSON
			viewModel.DataRowsLabels = JsonConvert.SerializeObject(labels);

			// set values in last data row to all 1's (ones)
			lastReportDataRow.Values = lastReportDataRow.Values.Select(v => 1m).ToList();

			// serialize the ReportData only after the labels, because we're setting the last values row to all 1's
			viewModel.DataRows = JsonConvert.SerializeObject(report.ReportData.Select(dataRow => dataRow.Values));
			return viewModel;
		}
	}
}