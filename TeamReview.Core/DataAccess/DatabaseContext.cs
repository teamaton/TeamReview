using System.Data.Entity;
using TeamReview.Core.Models;

namespace TeamReview.Core.DataAccess {
	public class DatabaseContext : DbContext, IDatabaseContext {
        public DatabaseContext(string connectionString = "name=DefaultConnection")
			: base(connectionString) {
		}

		public DbSet<UserProfile> UserProfiles { get; set; }
		public DbSet<ReviewConfiguration> ReviewConfigurations { get; set; }
		public DbSet<ReviewCategory> ReviewCategories { get; set; }
		public DbSet<ReviewFeedback> ReviewFeedbacks { get; set; }
		public DbSet<Assessment> Assessments { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder) {
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<ReviewConfiguration>()
				.HasOptional(c => c.Initiator)
				.WithOptionalDependent();
		}
	}
}