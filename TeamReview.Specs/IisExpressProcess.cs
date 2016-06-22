using System;
using System.Diagnostics;
using System.IO;

namespace TeamReview.Specs {
	internal class IisExpressProcess : BackgroundProcessBase {
		private readonly string _path;

		public IisExpressProcess(string directory, int port) {
			Port = port;
			_path = directory;
		}

		public int Port { get; private set; }

		public string BaseUrl {
			get { return "localhost"; }
		}

		protected override ProcessStartInfo GetProcessInfo() {
			var iisExpressPath = DetermineIisExpressPath();
			var arguments = String.Format(@"/path:""{0}"" /port:{1} /systray:false", _path, Port);
			Console.WriteLine(@"### Starting IIS Express: ""{0}"" {1}", iisExpressPath, arguments);

			return new ProcessStartInfo(iisExpressPath)
			       	{
			       		WindowStyle = ProcessWindowStyle.Hidden,
			       		ErrorDialog = true,
			       		LoadUserProfile = true,
			       		CreateNoWindow = false,
			       		UseShellExecute = false,
			       		Arguments = arguments,
			       		WorkingDirectory = Environment.CurrentDirectory
			       	};
		}

		/// <summary>
		/// Determines the IIS express path.
		/// </summary>
		/// <returns>
		/// A <see cref="String"/> instance. 
		/// </returns>
		private static String DetermineIisExpressPath() {
			String iisExpressPath;

			if (Environment.Is64BitOperatingSystem) {
				iisExpressPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			}
			else {
				iisExpressPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			}

			iisExpressPath = Path.Combine(iisExpressPath, @"IIS Express\iisexpress.exe");

			return iisExpressPath;
		}

		public override string ToString() {
			return string.Format("{3}# Path: {0}{3}# Url: {1}{3}# Port: {2}",
			                     _path, BaseUrl, Port, Environment.NewLine);
		}
	}
}