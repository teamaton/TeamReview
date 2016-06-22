using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TeamReview.Core.Models {
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class ReviewConfiguration {
		public const string UntitledName = "Untitled Review";
		private IList<ReviewCategory> _categories;
		private IList<UserProfile> _peers;

		private IList<ReviewRound> _reviewRounds;

		public int Id { get; set; }
		public bool Active { get; set; }

		[Required]
		public string Name { get; set; }
		public UserProfile Initiator { get; set; }

		[Required]
		public int LengthOfReviewRoundInWeeks { get; set; }
	
		public virtual IList<ReviewCategory> Categories {
			get { return _categories; }
			set { _categories = value; }
		}

		public virtual IList<UserProfile> Peers {
			get { return _peers; }
			set { _peers = value; }
		}

		/// <summary>
		/// If you want to access existing review rounds only, do not use this property directly. 
		/// Use <see cref="ReviewRoundsInOrder"/> instead. Needed here for EF and for adding new review rounds.
		/// </summary>
		public virtual IList<ReviewRound> ReviewRounds {
			get { return _reviewRounds; }
			set { _reviewRounds = value; }
		}

		public void EnsureName() {
			if (string.IsNullOrWhiteSpace(Name)) {
				Name = UntitledName;
			}
		}

		public ReviewConfiguration() {
			_categories = new List<ReviewCategory>();
			_peers = new List<UserProfile>();
			_reviewRounds = new List<ReviewRound>();
		}

		public IList<ReviewFeedback> GetCurrentFeedback() {
			var activeRound = GetCurrentReviewRound();
			return activeRound != null
				       ? activeRound.Feedback
				       : null;
		}

		public ReviewRound GetCurrentReviewRound() {
			return ReviewRounds.SingleOrDefault(round => round.Active);
		}

		public IEnumerable<ReviewRound> ReviewRoundsInOrder {
			get { return ReviewRounds.OrderBy(round => round.StartDate); }
		}
	}
}