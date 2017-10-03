using AppKit;
using Foundation;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Xamarin;
using QuickLookUI;

namespace DiskReport
{
	public class PreviewItem : QLPreviewItem
	{
		public override NSUrl PreviewItemURL {
			get {
				return new NSUrl (MainWindow._quick_look_path, true);
			}
		}
	}

	public partial class AppDelegate : NSApplicationDelegate, IQLPreviewPanelDataSource, IQLPreviewPanelDelegate
	{
		MainWindowController mainWindowController;

		public AppDelegate ()
		{
		}

		[Export ("acceptsPreviewPanelControl:")]
		public bool AcceptsPreviewPanelControl (QLPreviewPanel panel)
		{
			Console.WriteLine ("AcceptsPreviewPanelControl");
			return true;
		}

		[Export ("beginPreviewPanelControl:")]
		public void BeginPreviewPanelControl (QLPreviewPanel panel)
		{
			Console.WriteLine ("BeginPreviewPanelControl");

			panel.Delegate = this;
			panel.DataSource = this;
		}

		[Export ("endPreviewPanelControl:")]
		public void EndPreviewPanelControl (QLPreviewPanel panel)
		{
			Console.WriteLine ("EndPreviewPanelControl");
			panel.Delegate = null;
			panel.DataSource = null;
		}

		public nint NumberOfPreviewItemsInPreviewPanel (QLPreviewPanel panel)
		{
			return 1;
		}

		public IQLPreviewItem PreviewItemAtIndex (QLPreviewPanel panel, nint index)
		{
			return new PreviewItem ();
		}
			
		public override void DidFinishLaunching (NSNotification notification)
		{
			/*
			new System.Threading.Thread (() => 
				{
					while (true) {
						System.Threading.Thread.Sleep (1000);
						Debug("GC.Collect: " + DateTime.Now.ToString());
						GC.Collect ();
					}
				}).Start ();
				*/

			Insights.HasPendingCrashReport += (sender, isStartupCrash) =>
			{
				if (isStartupCrash) {
					Insights.PurgePendingCrashReports().Wait();
				}
			};				
				
			Insights.Initialize("39819b4f3c0a9fbe38391c6413f6a9d48e0d6cd5", "1.0", "Disk Reporter");

			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);
		}

		public override void WillTerminate (NSNotification notification)
		{
			// Insert code here to tear down your application
		}

		public static void Debug(
			string message = "HERE",
			[CallerLineNumber] int lineNumber = 0,
			[CallerMemberName] string caller = null,
			[CallerFilePath] string file = null)
		{
			Console.WriteLine(message + " at line " + lineNumber + " (" + caller + ") " + "[" + file + "]");
		}

		public static void DebugModal(
			NSWindow window,
			string message = "HERE",
			[CallerLineNumber] int lineNumber = 0,
			[CallerMemberName] string caller = null,
			[CallerFilePath] string file = null)
		{
			Console.WriteLine(message + " at line " + lineNumber + " (" + caller + ") " + "[" + file + "]");

			var alert = new NSAlert () {
				AlertStyle = NSAlertStyle.Informational,
				InformativeText = lineNumber + " (" + caller + ") " + "[" + file + "]",
				MessageText = message,
			};

			alert.BeginSheet (window);
		}
	}

	public class Globals
	{
		public static int MiB = 1048576;
	}
}