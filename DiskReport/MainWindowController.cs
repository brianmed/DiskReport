using System;

using Foundation;
using AppKit;

using CoreGraphics;

namespace DiskReport
{
	public partial class MainWindowController : NSWindowController
	{
		public MainWindowController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
		}

		public MainWindowController () : base ("MainWindow")
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		static public void _WillClose (object sender, EventArgs e) 
		{
			Environment.Exit(0);
		}

		public override void WindowDidLoad ()
		{
			base.WindowDidLoad ();

			this.Window.WillClose += _WillClose;

			var alert = new NSAlert () {
				AlertStyle = NSAlertStyle.Informational,
				MessageText = "Please have a backup",
				InformativeText = "Program testing can be used to show the presence of bugs, but never to show their absence!",
			};

			alert.BeginSheet (this.Window);

		}

		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}
	}
}
