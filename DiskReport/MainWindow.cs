using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using AppKit;
using ObjCRuntime;
using CoreGraphics;
using Security;
using QuickLookUI;
using ScriptingBridge;

using System.Reflection;
using System.IO;

using System.Diagnostics;

namespace DiskReport
{
	public class DiskItem
	{
		public string Path { get; set; }
		public long Size { get; set; }
	}

	public partial class MainWindow : NSWindow
	{
		static public SemaphoreSlim _once = new SemaphoreSlim(1);

		static public string _quick_look_path = null;

		List<DiskItem> listFilesFound = new List<DiskItem> ();

		public MainWindow (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public MainWindow (NSCoder coder) : base (coder)
		{
		}

		public void DelAction()
		{
			var trashedFile = Path.GetTempFileName ();

			var removeRows = new Dictionary<nuint, Entity> ();

			foreach (var row in FileResults.SelectedRows.ToArray()) {
				int converted;

				if (false == Int32.TryParse (row.ToString (), out converted)) {
					continue;
				}			

				var path = ((EntityDataSource)FileResults.DataSource).Entities [converted].Path;

				removeRows [row] = ((EntityDataSource)FileResults.DataSource).Entities [converted];

				System.IO.File.AppendAllText (trashedFile, path + '\n');
			}
				
			AppDelegate.Debug (trashedFile);

			var process = Process.Start ("/usr/bin/osascript", NSBundle.MainBundle.ResourcePath + "/santas_helper.scpt " + trashedFile);
			process.WaitForExit ();

			var rowsToVanish = new NSMutableIndexSet ();

			foreach (var key in removeRows.Keys) {
				// Maybe they hit cancel when authenticating
				if (Directory.Exists (removeRows[key].Path) || File.Exists (removeRows[key].Path)) {
					continue;
				}

				rowsToVanish.Add (key);

				((EntityDataSource)FileResults.DataSource)._entities.Remove (removeRows[key]);
			}
				
			BeginInvokeOnMainThread (() => FileResults.RemoveRows (rowsToVanish, NSTableViewAnimation.Fade));
		}

		partial void ClickedTrash (Foundation.NSObject sender)
		{
			if (0 == FileResults.SelectedRows.Count) {
				var alert = new NSAlert () {
					AlertStyle = NSAlertStyle.Warning,
					InformativeText = "Please select a path",
					MessageText = "Action Info",
				};

				alert.BeginSheet (this);

				return;
			}

			var ask = new NSAlert () {
				MessageText = "Are you sure?"
			};

			ask.AddButton("Move to Trash");
			ask.AddButton("Cancel");

			ask.BeginSheetForResponse(this, (result) => {
				if (1000 != result) {
					return;
				}

				DelAction();
			});
		}

		public delegate void SearchDelegate(NSObject sender);
		partial void ClickedSearch (Foundation.NSObject sender)
		{
			bool only_one = _once.Wait(1);

			if (false == only_one) {
				return;
			}
				
			this.SearchButton.Enabled = false;
			this.SearchParameter.Enabled = false;
			this.SelectableActions.Enabled = false;
			this.TrashCanIcon.Enabled = false;
			this.SearchField.Enabled = false;

			if (null == SearchParameter.SelectedValue) {
				var alert = new NSAlert () {
					AlertStyle = NSAlertStyle.Warning,
					InformativeText = "Please select a search criteria",
					MessageText = "",
				};

				alert.BeginSheet (this);

				this.SearchButton.Enabled = true;
				this.SearchParameter.Enabled = true;
				this.SelectableActions.Enabled = false;
				this.TrashCanIcon.Enabled = false;
				this.SearchField.Enabled = false;

				_once.Release();

				return;
			}

			EntityDataSource._searchForMe = null;

			var coolness = new Dictionary<string, SearchDelegate>();

			coolness["Large Files"] = SearchLargeFiles;
			coolness["Application Sizes"] = SearchApplicationSizes;
			coolness["Logs"] = SearchLogs;
			coolness["Caches"] = SearchCaches;

			coolness[SearchParameter.SelectedValue.ToString()](sender);
		}

		partial void SelectedAnAction (Foundation.NSObject sender)
		{
			if (0 == FileResults.SelectedRows.Count) {
				var alert = new NSAlert () {
					AlertStyle = NSAlertStyle.Warning,
					InformativeText = "Please select a path",
					MessageText = "Action Info",
				};

				alert.BeginSheet (this);

				return;

			}

			if (1 < FileResults.SelectedRows.Count) {
				var alert = new NSAlert () {
					AlertStyle = NSAlertStyle.Warning,
					InformativeText = "Please select just one path",
					MessageText = "Action Info",
				};

				alert.BeginSheet (this);

				return;
			}

			int row;
			if (false == Int32.TryParse(FileResults.SelectedRow.ToString(), out row)) {
				return;
			}				

			var path = ((EntityDataSource) FileResults.DataSource).Entities[row].Path;

			if ("Show in Finder" == SelectableActions.SelectedItem.Title) {
				NSWorkspace.SharedWorkspace.SelectFile(path, System.IO.Path.GetDirectoryName(path));
			}

			if ("Quick Look" == SelectableActions.SelectedItem.Title) {
				_quick_look_path = path;

				if (QLPreviewPanel.SharedPreviewPanelExists() && QLPreviewPanel.SharedPreviewPanel().IsVisible) {
					QLPreviewPanel.SharedPreviewPanel().ReloadData();

					// QLPreviewPanel.SharedPreviewPanel().OrderOut(null);
				}
				else {
					QLPreviewPanel.SharedPreviewPanel().MakeKeyAndOrderFront(null);
				}
			}
		}

		#if __JOY
		var panel = NSOpenPanel.OpenPanel;
		panel.FloatingPanel = true;
		panel.CanChooseDirectories = true;
		panel.CanChooseFiles = true;
		panel.AllowedFileTypes = new string[] { "tiff", "jpeg", "jpg", "gif", "png" };
		#endif

		public void DirSearch(string sDir, string pattern) 
		{
			try	
			{
				foreach (string d in Directory.GetDirectories(sDir)) 
				{
					foreach (string f in Directory.GetFiles(d, pattern)) 
					{
						var finfo = new System.IO.FileInfo(f);

						var entity = new DiskItem();
						entity.Path = f;
						entity.Size = finfo.Length;

						this.listFilesFound.Add(entity);
					}
					DirSearch(d, pattern);
				}
			}
			catch (System.Exception excpt) 
			{
				Console.WriteLine(excpt.Message);
			}
		}

		public async void SearchCaches(NSObject sender)
		{
			IsAppWorking.StartAnimation (sender);

			listFilesFound.Clear ();

			await Task.Run (() => {
				DirSearch ("/Library/Caches", "*");
				DirSearch (System.Environment.GetEnvironmentVariable ("HOME") + "/Library/Caches", "*");
			});

			BeginInvokeOnMainThread (() => {
				loadResultsFromDiskItems ();

				IsAppWorking.StopAnimation (sender);
			});
		}

		public async void SearchLogs(NSObject sender)
		{
			IsAppWorking.StartAnimation (sender);

			listFilesFound.Clear ();

			await Task.Run (() => {
				DirSearch ("/Library/Logs", "*.log");
				DirSearch (System.Environment.GetEnvironmentVariable ("HOME") + "/Library/Logs", "*.log");
				DirSearch ("/private/var/log", "*");
			});

			BeginInvokeOnMainThread (() => {
				loadResultsFromDiskItems ();

				IsAppWorking.StopAnimation (sender);
			});
		}

		public void SearchLargeFiles(NSObject sender)
		{
			var query = new NSMetadataQuery();

			var nf = NSNotificationCenter.DefaultCenter;
			nf.AddObserver (this, new Selector ("queryNotification:"), null, query);

			NSPredicate predicate = NSPredicate.FromFormat ("kMDItemFSSize > " + 100 * Globals.MiB, new NSObject[0]);
			// NSPredicate predicate = NSCompoundPredicate.CreateAndPredicate (new NSPredicate[2] {addrBookPredicate, predicate});

			query.Predicate = predicate;

			query.SearchScopes = new NSObject[]{ NSMetadataQuery.LocalComputerScope };

			IsAppWorking.StartAnimation (sender);

			query.StartQuery();
		}

		public void SearchApplicationSizes(NSObject sender)
		{
			var query = new NSMetadataQuery();

			var nf = NSNotificationCenter.DefaultCenter;
			nf.AddObserver (this, new Selector ("queryNotification:"), null, query);

			NSPredicate predicate = NSPredicate.FromFormat ("kMDItemContentType = 'com.apple.application-bundle'", new NSObject[0]);
			// NSPredicate predicate = NSCompoundPredicate.CreateAndPredicate (new NSPredicate[2] {addrBookPredicate, predicate});

			query.Predicate = predicate;

			query.SearchScopes = new NSObject[]{ NSMetadataQuery.LocalComputerScope };

			IsAppWorking.StartAnimation (sender);

			query.StartQuery();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			this.SearchButton.Enabled = true;
			this.SearchParameter.Enabled = true;
			this.SelectableActions.Enabled = false;
			this.TrashCanIcon.Enabled = false;
			this.SearchField.Enabled = false;

			this.SearchField.EditingEnded += delegate (object sender, EventArgs e) {
				var n = (NSNotification)sender;

				NSTextView textView = (NSTextView)n.UserInfo.ObjectForKey ((NSString) "NSFieldEditor");

				if (String.IsNullOrEmpty(textView.Value)) {
					EntityDataSource._searchForMe = null;
				}
				else {
					EntityDataSource._searchForMe = textView.Value;
				}

				FileResults.ReloadData();
			};

			/*
		    var trashMenuItem = new NSMenuItem (String.Format ("Move to Trash"), delegate {
				AppDelegate.DebugModal(this, "Trash");
		    });
		 
			SelectableActions.Menu.AddItem (trashMenuItem);
			*/

			FileResults.Delegate = new EntityTableDelegate ();
		}

		[Export ("queryNotification:")]
		async public void queryNotification (NSNotification note) 
		{
			// the NSMetadataQuery will send back a note when updates are happening. By looking at the [note name], we can tell what is happening

			// the query has just started
			if (note.Name == NSMetadataQuery.DidStartGatheringNotification) {
				/* Nothing here yet */
			}

			// at this point, the query will be done. You may recieve an update later on.
			if (note.Name == NSMetadataQuery.DidFinishGatheringNotification) {				
				await Task.Run(() => loadResultsFromQuery (note));

				IsAppWorking.StopAnimation (this);
			} 

			// the query is still gathering results...
			if (note.Name == NSMetadataQuery.GatheringProgressNotification){
				// Console.WriteLine ("...");
			}

			// an update will happen when Spotlight notices that a file as added, removed, or modified that affected the search results.
			if (note.Name == NSMetadataQuery.DidUpdateNotification) {
				// Intentionally left blank
			}
		}

		public class Entity
		{
			public string Path { get; set;} = "";
			public Int64 Size { get; set;} = 0;

			public Entity ()
			{
			}

			public Entity (string path, Int64 size)
			{
				this.Path = path;
				this.Size = size;
			}
		}

		public class EntityDataSource : NSTableViewDataSource
		{
			public List<Entity> _entities = new List<Entity>();

			public List<Entity> Entities {
				get {
					if (null == EntityDataSource._searchForMe) {
						return _entities;
					} else {
						return _entities.Where ((Entity e) => e.Path.Contains (EntityDataSource._searchForMe)).ToList ();
					}
				}

				set {
					_entities = value;
				}
			}

			static public string _searchForMe = null;

			public EntityDataSource ()
			{
			}
				
			public override nint GetRowCount (NSTableView tableView)
			{
				return Entities.Count;
			}

			public void Sort(string key, bool ascending) {
				// Take action based on key
				switch (key) {
				case "Size":
					if (ascending) {
						Entities.Sort ((x, y) => x.Size.CompareTo (y.Size));
					} else {
						Entities.Sort ((x, y) => -1 * x.Size.CompareTo (y.Size));
					}
					break;
				case "Path":
					if (ascending) {
						Entities.Sort ((x, y) => x.Path.CompareTo (y.Path));
					} else {
						Entities.Sort ((x, y) => -1 * x.Path.CompareTo (y.Path));
					}
					break;
				}
			}

			public override void SortDescriptorsChanged (NSTableView tableView, NSSortDescriptor[] oldDescriptors)
			{
				// Sort the data
				if (oldDescriptors.Length > 0) {
					// Update sort
					Sort (oldDescriptors [0].Key, oldDescriptors [0].Ascending);
				} else {
					// Grab current descriptors and update sort
					NSSortDescriptor[] tbSort = tableView.SortDescriptors; 
					Sort (tbSort[0].Key, tbSort[0].Ascending); 
				}

				// Refresh table
				tableView.ReloadData ();
			}
		}

		public class EntityTableDelegate: NSTableViewDelegate
		{
			// private EntityDataSource DataSource;

			public static string _searchString = null;

			public EntityTableDelegate ()
			{
				// this.DataSource = datasource;
			}

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				EntityDataSource ds = tableView.DataSource as EntityDataSource;

				// This pattern allows you reuse existing views when they are no-longer in use.
				// If the returned view is null, you instance up a new view
				// If a non-null view is returned, you modify it enough to reflect the new data
				// NSTableCellView view = (NSTableCellView)tableView.MakeView (tableColumn.Title, this);

				NSTableCellView view = null;

				if (view == null) {
					view = new NSTableCellView ();

					if (tableColumn.Title == "Path") {
						view.ImageView = new NSImageView (new CGRect (0, 0, 16, 16));
						view.AddSubview (view.ImageView);
						view.TextField = new NSTextField (new CGRect (20, 0, 400, 16));
					} else {
						view.TextField = new NSTextField (new CGRect (0, 0, 400, 16));
						// view.TextField.Formatter = new NSByteCountFormatter ();
					}

					view.TextField.AutoresizingMask = NSViewResizingMask.WidthSizable;
					view.AddSubview (view.TextField);

					view.Identifier = tableColumn.Title;
					view.TextField.BackgroundColor = NSColor.Clear;
					view.TextField.Bordered = false;
					view.TextField.Selectable = false;
					view.TextField.Editable = false;
				}

				// Setup view based on the column selected
				switch (tableColumn.Title) {
				case "Size":

					var formatter = new NSByteCountFormatter ();
					formatter.CountStyle = NSByteCountFormatterCountStyle.File;
					formatter.ZeroPadsFractionDigits = true;
					formatter.Adaptive = false;

					view.TextField.StringValue = formatter.Format (ds.Entities [(int)row].Size);

					break;
				case "Path":
					var path = ds.Entities [(int)row].Path;
					view.TextField.StringValue = path;

					if (path.StartsWith ("/Applications/") || path.StartsWith ("/System/Library") || path.StartsWith ("/Library") || path.StartsWith("/usr/")) {
						view.ImageView.Image = NSImage.ImageNamed (NSImageName.Caution);
					}
					break;
				}

				return view;
			}

			public override bool SelectionShouldChange (NSTableView tableView)
			{
				((MainWindow)tableView.Window).TrashCanIcon.Image = NSImage.ImageNamed (NSImageName.TrashFull);

				((MainWindow)tableView.Window).TrashCanIcon.Enabled = true;

				return true;
			}
		}

		private void loadResultsFromDiskItems() 
		{
			BeginInvokeOnMainThread(() => {
				((MainWindow)FileResults.Window).TrashCanIcon.Image = NSImage.ImageNamed (NSImageName.TrashEmpty);
				((MainWindow)FileResults.Window).TrashCanIcon.Enabled = false;

				SearchField.Enabled = false;

				FileResults.DataSource = new EntityDataSource();

				FileResults.ReloadData();
			});

			// var results = new List<NSMetadataItem> (((NSMetadataQuery)notif.Object).Results);

			listFilesFound.Sort(delegate (DiskItem x, DiskItem y){
				return y.Size.CompareTo(x.Size);
			});

			var ds = new EntityDataSource ();

			// iterate through the array of results, and match to the existing stores
			foreach (DiskItem item in listFilesFound) {
				ds.Entities.Add (new Entity (item.Path, item.Size));
			}

			// NSWorkspace.SharedWorkspace.OpenUrl(aURL)

			// NSWorkspace.SharedWorkspace.SelectFile(

			BeginInvokeOnMainThread (() => {
				FileResults.UsesAlternatingRowBackgroundColors = true;
				FileResults.DataSource = ds;
				FileResults.AllowsMultipleSelection = true;

				this.SearchButton.Enabled = true;
				this.SearchParameter.Enabled = true;
				this.SelectableActions.Enabled = true;
				this.TrashCanIcon.Enabled = true;
				this.SearchField.Enabled = true;

				if (false == String.IsNullOrEmpty(SearchField.StringValue)) {
					EntityDataSource._searchForMe = SearchField.StringValue;

					FileResults.ReloadData();
				}

				_once.Release();
			});
		}

		private void loadResultsFromQuery (NSNotification notif)
		{
			BeginInvokeOnMainThread(() => {
				((MainWindow)FileResults.Window).TrashCanIcon.Image = NSImage.ImageNamed (NSImageName.TrashEmpty);
				((MainWindow)FileResults.Window).TrashCanIcon.Enabled = false;

				SearchField.Enabled = false;

				FileResults.DataSource = new EntityDataSource();

				FileResults.ReloadData();
			});

			var results = new List<NSMetadataItem> (((NSMetadataQuery)notif.Object).Results);

			results.Sort (delegate (NSMetadataItem x, NSMetadataItem y){
				if (null != x.FileSystemSize && null != y.FileSystemSize) {
					var a = Int64.Parse(x.FileSystemSize.ToString());
					var b = Int64.Parse(y.FileSystemSize.ToString());

					return b.CompareTo(a);
				}

				return 0;
			});
						
			var ds = new EntityDataSource ();

			// iterate through the array of results, and match to the existing stores
			foreach (NSMetadataItem item in results) {
				Int64 parsed = 0;
				bool ok;

				if (null != item && null != item.FileSystemSize) {
					ok = Int64.TryParse (item.FileSystemSize.ToString (), out parsed);

					if (ok) {
						ds.Entities.Add (new Entity (item.Path, parsed));
					} else {
						AppDelegate.Debug (item.FileSystemSize.ToString ());
					}
				}
			}

			results.Clear ();

			// NSWorkspace.SharedWorkspace.OpenUrl(aURL)

			// NSWorkspace.SharedWorkspace.SelectFile(

			BeginInvokeOnMainThread (() => {
				FileResults.UsesAlternatingRowBackgroundColors = true;
				FileResults.DataSource = ds;
				FileResults.AllowsMultipleSelection = true;

				this.SearchButton.Enabled = true;
				this.SearchParameter.Enabled = true;
				this.SelectableActions.Enabled = true;
				this.TrashCanIcon.Enabled = true;
				this.SearchField.Enabled = true;

				if (false == String.IsNullOrEmpty(SearchField.StringValue)) {
					EntityDataSource._searchForMe = SearchField.StringValue;

					FileResults.ReloadData();
				}

				_once.Release();
			});
		}
	}
}