using System.Data.Entity;
using TeamReview.Core.Models;

namespace TeamReview.Core.DataAccess {
	public interface IDatabaseContext {
		DbSet<UserProfile> UserProfiles { get; set; }
		DbSet<ReviewConfiguration> ReviewConfigurations { get; set; }
		DbSet<ReviewCategory> ReviewCategories { get; set; }
		DbSet<ReviewFeedback> ReviewFeedbacks { get; set; }
		DbSet<Assessment> Assessments { get; set; }

		int SaveChanges();
	}
}