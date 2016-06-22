using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace TeamReview.Specs {
	internal abstract class BackgroundProcessBase : IDisposable {
		/// <summary>
		/// Stores whether this instance has been disposed.
		/// </summary>
		private bool _isDisposed;

		/// <summary>
		/// Stores the process instance.
		/// </summary>
		private Process _process;

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources. 
		/// </param>
		protected virtual void Dispose(bool disposing) {
			if (_isDisposed) {
				return;
			}

			if (disposing) {
				// Free managed resources
				if (_process.HasExited == false) {
					_process.CloseMainWindow();
					Thread.Sleep(500);
					try {
						_process.Kill();
					}
					catch (Exception) {
					}
				}

				_process.Dispose();
			}

			// Free native resources if there are any
			_isDisposed = true;
		}

		/// <summary>
		/// Starts a new <see cref="Thread"/> that executes the given process.
		/// </summary>
		public void Start() {
			var info = GetProcessInfo();

			var startThread = new Thread(() => StartProcess(info))
			                  	{
			                  		IsBackground = true
			                  	};

			startThread.Start();
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
			Justification = "Required here to ensure that the instance is disposed.")]
		private void StartProcess(ProcessStartInfo info) {
			try {
				_process = Process.Start(info);
				_process.WaitForExit();
			}
			catch (Exception) {
				Dispose();
			}
		}

		protected abstract ProcessStartInfo GetProcessInfo();
	}
}