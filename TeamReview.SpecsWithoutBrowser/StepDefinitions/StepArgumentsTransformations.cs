using System.Collections.Generic;
using TeamReview.SpecsWithoutBrowser.StepDefinitions.Models;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace TeamReview.SpecsWithoutBrowser.StepDefinitions {
	[Binding]
	public class StepArgumentsTransformations {
		[StepArgumentTransformation]
		public IEnumerable<AssessmentInfo> AssessmentInfosTransform(Table table) {
			return table.CreateSet<AssessmentInfo>();
		}
	}
}
