using System;
using TechTalk.SpecFlow;

namespace TeamReview.Specs {
	[Binding]
	public class TestSteps {
		[Given(@"I have one argument ([^ ]*)")]
		[Given(@"I have one argument ""([^""]*)""")]
		public void GivenIHaveOneArgument(string aaa) {
			GivenIHaveOneArgument(aaa, null);
		}

		[Given(@"I have one argument ([^ ]*) and another argument (.*)")]
		public void GivenIHaveOneArgument(string aaa, string bbb)
		{
			Console.WriteLine(aaa);
			Console.WriteLine(bbb);
		}
	}
}