using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using NUnit.Framework;
using TeamReview.Core.Models;
using TeamReview.SpecsWithoutBrowser.StepDefinitions.Models;
using TeamReview.Web.ViewModels;
using TechTalk.SpecFlow;

namespace TeamReview.SpecsWithoutBrowser.StepDefinitions {
	[Binding]
	[Scope(Feature = "Review reports")]
	public class ReviewReportsSteps : StepsBase {
		public ReviewReportsSteps(ReviewInfo reviewInfo, CommonContext commonContext)
			: base(reviewInfo, commonContext) {
		}

		[When(@"I create a report for peers")]
		public void WhenICreateAReportForPeers() {
			_reviewInfo.TypeOfReport = "PeerReport";
		}

		[When(@"I choose (.*) for peer and (.*) for category")]
		public void WhenIChoosePeerAndCategory(string peer, string category) {
			if (_reviewInfo.TypeOfReport == "PeerReport") {
				var review = _reviewInfo.ReviewConfiguration;
				var categoryId = review.Categories.First(c => c.Name == category).Id;
				var selectedPeerIds = SelectedPeerIds(peer, review);

				_reviewInfo.ReportViewModel = (ReportViewModel)
				                              ((ViewResultBase)
				                               _context.ReportController.Value.PeerReport(review.Id, selectedPeerIds, categoryId))
					                              .Model;
			}
			else {
				ScenarioContext.Current.Pending();
			}
		}

		private static List<int> SelectedPeerIds(string peer, ReviewConfiguration review) {
			var peers = Regex.Split(peer, @" and ");
			return peers.Select(userName => review.Peers.First(p => p.UserName == userName).Id).ToList();
		}

		[Then(@"I get a peer report containing productivity data for Jeb")]
		public void ThenIGetAPeerReportContainingProductivityDataForJeb() {
			var review = _reviewInfo.ReviewConfiguration;
			var viewModel = _reviewInfo.ReportViewModel;

			Assert.True(viewModel.Title.Contains("productivity"),
						"Title should contain the category 'productivity'!");
			Assert.True(viewModel.Title.Contains("Jeb"),
						"Title should contain the selected user name (Jeb)!");
			Assert.AreEqual(review.ReviewRounds.Count, viewModel.DataRows.Split(',').Count(),
							"There should be two review rounds!");
			Assert.True(viewModel.DataRows.Contains("5.0,6.0"),
						"The data row should contain the correct 'productivity' values for Jeb: 5.0 and 6.0!");
		}

		[Then(@"I get a peer report containing performance data for Jeb and Mel")]
		public void ThenIGetAPeerReportContainingPerformanceDataForJebAndMel() {
			var viewModel = _reviewInfo.ReportViewModel;

			Assert.True(viewModel.Title.Contains("performance"),
						"Title should contain the category 'performance'!");
			Assert.True(viewModel.Title.Contains("Jeb"),
						"Title should contain the selected user name (Jeb)!");
			Assert.True(viewModel.Title.Contains("Mel"),
						"Title should contain the selected user name (Mel)!");
			Assert.True(viewModel.DataRows.Contains("[3.0,5.0],[4.0,7.0]"),
						"The data row should contain the correct 'performance' values for Jeb: 3.0 and 5.0 and for Mel: 4.0 and 7.0!");
		}

		[When(@"I create a report for myself")]
		public void WhenICreateAReportForMyself() {
			_reviewInfo.TypeOfReport = "OwnReport";
		}

		[When(@"I create a team report")]
		public void WhenICreateATeamReport() {
			_reviewInfo.TypeOfReport = "TeamReport";
		}

		[When(@"I choose '(.*)' for category")]
		public void WhenIChooseACategory(string category) {
			var review = _reviewInfo.ReviewConfiguration;
			_reviewInfo.CurrentCategory = review.Categories.First(c => c.Name == category);

			switch (_reviewInfo.TypeOfReport) {
				case "OwnReport":
					_reviewInfo.ReportViewModel = (ReportViewModel)
					                              ((ViewResultBase)
					                               _context.ReportController.Value.OwnReport(review.Id, _reviewInfo.CurrentCategory.Id))
						                              .Model;
					break;
				case "TeamReport":
					_reviewInfo.ReportViewModel = (ReportViewModel)
					                              ((ViewResultBase)
					                               _context.ReportController.Value.TeamReport(review.Id, _reviewInfo.CurrentCategory.Id))
						                              .Model;
					break;
			}
		}

		[Then(@"I get a performance report with my own ratings compared with ratings of me by the team")]
		public void ThenIGetAPerformanceReportWithMyOwnRatingsComparedWithRatingsOfMeByTheTeam() {
			var viewModel = _reviewInfo.ReportViewModel;

			Assert.AreEqual(1, viewModel.CategoryCount,
				"The team report should contain one category!");
			Assert.True(viewModel.Title.Contains("performance"),
			            "Title should contain the category 'performance'!");
			Assert.True(viewModel.Title.Contains("My own report"),
			            "Title should contain the type 'My own report'!");
			Assert.True(viewModel.DataRows.Contains("[4.0,5.0],[3.0,5.0]"),
			            "The data row should contain the correct 'performance' values for me " +
			            "rated by myself: 4.0 and 5.0 and rated by the team: 3.0 and 5.0!");
		}

		[Then(@"I get a team report with productivity data for my team")]
		public void ThenIGetATeamReportWithPerformanceDataForMyTeam() {
			var viewModel = _reviewInfo.ReportViewModel;

			Assert.AreEqual(1, viewModel.CategoryCount,
				"The team report should contain one category!");
			Assert.True(viewModel.Title.Contains("productivity"),
				"Title should contain the category 'productivity'!");
			Assert.True(viewModel.DataRowsLabels.Contains("team (by team)"),
				"Legend labels should contain the type 'team (by team)'!");
			Assert.True(viewModel.DataRows.Contains("[5.0,6.0]"),
				"The data row should contain the correct 'productivity' values for the team " +
				"and both rounds: 5.0 and 6.0!");
			}


		[When(@"I create a stack report for myself")]
		public void WhenICreateAStackReportForMyself() {
			var review = _reviewInfo.ReviewConfiguration;
			_reviewInfo.ReportViewModel = (ReportViewModel)
			                              ((ViewResultBase) _context.ReportController.Value.OwnStackRating(review.Id))
				                              .Model;
		}

		[Then(@"I get a stack ratings report with my own ratings compared with ratings of me by the team")]
		public void ThenIGetAStackRatingsReportWithMyOwnRatingsComparedWithRatingsOfMeByTheTeam() {
			var viewModel = _reviewInfo.ReportViewModel;
			
			Assert.True(viewModel.Title.Contains("My own stack ratings and me being rated by the team"),
				"Title should contain 'My own stack ratings and me being rated by the team'!");
			Assert.True(viewModel.DataRows.Contains("[4.0,3.0,5.0,5.0],[4.0,5.0,5.0,6.0]"),
				"The data row should contain the correct ratings for me in performance: '4.0,3.0,5.0,5.0' " +
				" and in productivity: '4.0,5.0,5.0,6.0'!");
			Assert.True(viewModel.DataRowsLabels.Contains("performance"),
						"The data row should contain labels for category 'performance'.");
			Assert.True(viewModel.DataRowsLabels.Contains("productivity"),
						"The data row should contain labels for category 'productivity'.");
			Assert.True(viewModel.DataRowsLabels.Contains("Total"),
						"The data row should contain labels for the sums ('Total').");
			Assert.True(viewModel.DataRowsLabels.Contains(@"""∑ 8"",""∑ 8"",""∑ 10"",""∑ 11"""),
				@"The data row should contain the correct sums as texts ('""∑ 8"",""∑ 8"",""∑ 10"",""∑ 11""').");
		}

		[When(@"I create a stack report for my team")]
		public void WhenICreateAStackedReportForMyTeam() {
			var review = _reviewInfo.ReviewConfiguration;
			_reviewInfo.ReportViewModel = (ReportViewModel)
										  ((ViewResultBase)_context.ReportController.Value.TeamStackRating(review.Id))
											  .Model;
		}

		[Then(@"I get a stack ratings report with summed ratings for each peer")]
		public void ThenIGetAStackRatingsReportWithSummedRatingsForEachPeer() {
			var review = _reviewInfo.ReviewConfiguration;
			var viewModel = _reviewInfo.ReportViewModel;

			Assert.True(viewModel.Title.Contains("Stack Ratings for all peers over all categories"),
				"Title should contain 'Stack Ratings for all peers over all categories'!");
			Assert.True(viewModel.DataRows.Contains("[8.0,11.0],[9.0,13.0],[8.0,11.0]"),
				"The data row should contain the correct summed ratings for both categories " +
				"and each of the three peers: '[8.0,11.0],[9.0,13.0],[8.0,11.0]'.");
			Assert.True(viewModel.DataRowsLabels.Contains(@"""∑ 25"",""∑ 35"""),
				@"The data row should contain the correct sums as texts ('""∑ 25"",""∑ 35""').");
			foreach (var peer in review.Peers) {
				Assert.True(viewModel.DataRowsLabels.Contains(peer.UserName),
					string.Format("The team report should contain data for peer '{0}'.", peer.UserName));
			}
			
		}
	}
}