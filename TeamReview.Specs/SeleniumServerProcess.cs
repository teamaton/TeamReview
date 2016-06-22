using System;
using System.Diagnostics;

namespace TeamReview.Specs {
	internal class SeleniumServerProcess : BackgroundProcessBase {
		protected override ProcessStartInfo GetProcessInfo() {
			return new ProcessStartInfo("java")
			       	{
			       		WindowStyle = ProcessWindowStyle.Hidden,
			       		ErrorDialog = true,
			       		LoadUserProfile = true,
			       		CreateNoWindow = false,
			       		UseShellExecute = false,
			       		Arguments = "-jar selenium-server-standalone-2.31.0.jar",
			       		WorkingDirectory = Environment.CurrentDirectory
			       	};
		}
	}
}