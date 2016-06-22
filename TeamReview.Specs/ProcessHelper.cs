using System;
using System.Diagnostics;

namespace TeamReview.Specs {
	public static class ProcessHelper {
		/// <summary>
		/// Starts a new Process instance with the given parameters and returns the started instance.
		/// Throws an ApplicationException when Start() failed.
		/// </summary>
		/// <param name="exe">The full path to the executable to start, or simply the file name if it's contained in PATH.</param>
		/// <param name="args">All parameters to pass to the exe as a single string.</param>
		/// <param name="workingDir">The initial directory in which to start the exe.</param>
		/// <param name="redirect">Whether or not to redirect the standard input and output streams of the new process.</param>
		/// <returns>A started Process instance.</returns>
		public static Process StartInteractive(string exe, string args = "", string workingDir = "", bool redirect = true) {
			var startInfo = new ProcessStartInfo
			                	{
			                		UseShellExecute = false,
			                		FileName = exe,
			                		Arguments = args,
			                		RedirectStandardOutput = redirect,
			                		RedirectStandardInput = redirect,
			                		RedirectStandardError = redirect,
			                		CreateNoWindow = false
			                	};
			if (!string.IsNullOrWhiteSpace(workingDir))
				startInfo.WorkingDirectory = workingDir;

			var process = new Process { StartInfo = startInfo };
			var started = process.Start();

			if (!started)
				throw new ApplicationException(string.Format("The new process could not be started! ({0}, {1}, {2})",
				                                             exe, args, workingDir));
			if (redirect)
				process.StandardInput.AutoFlush = true;

			return process;
		}
	}
}