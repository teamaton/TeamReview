using Coypu;
using Coypu.Drivers;
using NUnit.Framework;

namespace TeamReview.Specs {
	[TestFixture]
	public class HtmlUnitTest {
		[Test]
		public void CheckSimplePageLoad() {
			var sessionConfiguration = new SessionConfiguration
			                           	{
			                           		AppHost = "google.com",
			                           		Browser = Browser.HtmlUnitWithJavaScript,
			                           	};
			var browserSession = new BrowserSession(sessionConfiguration);
			browserSession.Visit("/");
			Assert.That(browserSession.HasContent("building web applications that work"));
		}
	}
}