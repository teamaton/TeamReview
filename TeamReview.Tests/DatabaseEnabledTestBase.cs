using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using NUnit.Framework;
using TeamReview.Core.DataAccess;

namespace TeamReview.Tests {
	public class DatabaseEnabledTestBase {
		protected IDatabaseContext DatabaseContext { get; private set; }
		private string _connectionString;

		[SetUp]
		public virtual void SetupTest() {
			CreateNewDatabase();
			CreateNewContext();
		}

		/// <summary>
		/// Original example: 
		/// http://www.codeproject.com/Articles/460175/Two-strategies-for-testing-Entity-Framework-Effort
		/// </summary>
		private void CreateNewDatabase() {
			const string databaseFileName = "ReviewTestDatabase.sdf";

			var filePath = Path.Combine(Environment.CurrentDirectory, databaseFileName);
			if (File.Exists(filePath)) {
				File.Delete(filePath);
			}

			_connectionString = "Datasource = " + filePath;

			// needed for SQL CE databases
			Database.DefaultConnectionFactory =
				new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");

			using (var context = new DatabaseContext(_connectionString)) {
				context.Database.Create();
			}
		}

		protected void CreateNewContext() {
			DatabaseContext = new DatabaseContext(_connectionString);
		}
	}
}