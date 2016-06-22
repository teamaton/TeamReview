using TeamReview.SpecsWithoutBrowser.StepDefinitions.Models;
using TechTalk.SpecFlow;

namespace TeamReview.SpecsWithoutBrowser.StepDefinitions
{
	[Binding]
	public class StepsBase
	{
		protected readonly ReviewInfo _reviewInfo;
		protected readonly CommonContext _context;

		protected StepsBase(ReviewInfo reviewInfo, CommonContext commonContext)
		{
			_reviewInfo = reviewInfo;
			_context = commonContext;
		}
	}
}