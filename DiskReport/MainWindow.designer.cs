// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace DiskReport
{
	[Register ("MainWindow")]
	partial class MainWindow
	{
		[Outlet]
		AppKit.NSScrollView EntitySizes { get; set; }

		[Outlet]
		AppKit.NSTableView FileResults { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator IsAppWorking { get; set; }

		[Outlet]
		AppKit.NSButton SearchButton { get; set; }

		[Outlet]
		AppKit.NSSearchField SearchField { get; set; }

		[Outlet]
		AppKit.NSComboBox SearchParameter { get; set; }

		[Outlet]
		AppKit.NSPopUpButton SelectableActions { get; set; }

		[Outlet]
		AppKit.NSButton TrashCanIcon { get; set; }

		[Action ("ClickedSearch:")]
		partial void ClickedSearch (Foundation.NSObject sender);

		[Action ("ClickedTrash:")]
		partial void ClickedTrash (Foundation.NSObject sender);

		[Action ("SearchingResults:")]
		partial void SearchingResults (Foundation.NSObject sender);

		[Action ("SelectedAnAction:")]
		partial void SelectedAnAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (EntitySizes != null) {
				EntitySizes.Dispose ();
				EntitySizes = null;
			}

			if (FileResults != null) {
				FileResults.Dispose ();
				FileResults = null;
			}

			if (IsAppWorking != null) {
				IsAppWorking.Dispose ();
				IsAppWorking = null;
			}

			if (SearchButton != null) {
				SearchButton.Dispose ();
				SearchButton = null;
			}

			if (SearchParameter != null) {
				SearchParameter.Dispose ();
				SearchParameter = null;
			}

			if (SelectableActions != null) {
				SelectableActions.Dispose ();
				SelectableActions = null;
			}

			if (TrashCanIcon != null) {
				TrashCanIcon.Dispose ();
				TrashCanIcon = null;
			}

			if (SearchField != null) {
				SearchField.Dispose ();
				SearchField = null;
			}
		}
	}
}
