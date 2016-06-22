using System;
using System.Collections.Generic;

namespace TeamReview.Core.Models {
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class ReviewRound {
		private IList<ReviewFeedback> _feedback;

		public ReviewRound() {
			_feedback = new List<ReviewFeedback>();
		}

		public virtual IList<ReviewFeedback> Feedback {
			get { return _feedback; }
			set { _feedback = value; }
		}

		public int Id { get; set; }
		public bool Active { get; set; }
		public DateTime StartDate { get; set; }
		public ReviewConfiguration ReviewConfiguration;
	}

	public class ReviewCategory {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}

	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class ReviewFeedback {
		private IList<Assessment> _assessments;

		public ReviewFeedback() {
			_assessments = new List<Assessment>();
		}

		public int Id { get; set; }
		public virtual UserProfile Reviewer { get; set; }

		public virtual IList<Assessment> Assessments {
			get { return _assessments; }
			set { _assessments = value; }
		}
	}

	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class Assessment {
		public int Id { get; set; }
		public virtual UserProfile Reviewer { get; set; }
		public virtual ReviewCategory ReviewCategory { get; set; }
		public virtual UserProfile ReviewedPeer { get; set; }
		public int Rating { get; set; }
	}
}