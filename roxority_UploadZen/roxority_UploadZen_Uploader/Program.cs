
using Microsoft.VisualBasic.Devices;
using roxority.Shared.ComponentModel;
using roxority.Shared.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows.Forms;

using TypeDesc = roxority.Shared.ComponentModel.CustomTypeDescriptor;

namespace roxUp {

	using roxority_UploadZen;
	using res = Properties.Resources;

	#region ILauncher Interface

	[Guid ("27B8A66F-3270-4b2a-8AA7-4AB2DB5E85B4")]
	public interface ILauncher {

		void Launch ();

	}

	#endregion

	#region MetaDataWrap Class

	public class MetaDataWrap : TypeDesc {

		public readonly WssSiteData._sProperty Prop;

		public MetaDataWrap (WssSiteData._sProperty prop) {
			Prop = prop;
		}

		public ListViewItem ToListViewItem () {
			ListViewItem item = new ListViewItem (new string [] { Prop.Title, Prop.Name, Prop.Type });
			item.Tag = this;
			return item;
		}

		public override string ToString () {
			return string.Format ("\"{0}\" ({1} -- {2})", Prop.Title, Prop.Type, Prop.Name);
		}

	}

	#endregion

	#region Program Class

	public static class Program {

		[STAThread]
		public static void Main (string [] args) {
			BooleanTypeConverter.FalseString = res.Bool_False;
			BooleanTypeConverter.TrueString = res.Bool_True;
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			Application.Run (new MainForm (args));
		}

	}

	#endregion

	#region Settings Class

	public class Settings : TypeDesc, IPropertyHelper {

		public static readonly ComputerInfo ComputerInfo = new ComputerInfo ();
		public static readonly string SPInvalidFileNameCharacters = "~#%&*:<>?{|}/\\\"";

		public readonly MainForm MainForm;

		private bool overwrite = false, suggestFiltersDocLibs = true, suggestFiltersOtherLibs = true, browseArchiveDirs = true, browseArchiveFiles = true, zipArchiveDirs = true, zipArchiveFiles = true, browseCompressedDirs = true, browseCompressedFiles = true, zipCompressedDirs = true, zipCompressedFiles = true, browseEmptyDirs = false, browseEmptyFiles = false, zipEmptyDirs = false, zipEmptyFiles = false, browseEncryptedDirs = true, browseEncryptedFiles = true, zipEncryptedDirs = true, zipEncryptedFiles = true, browseErrorDirs = false, browseErrorFiles = false, zipErrorDirs = false, zipErrorFiles = false, browseBlockedFiles = false, zipBlockedFiles = false, browseHiddenDirs = false, browseHiddenFiles = false, zipHiddenDirs = false, zipHiddenFiles = false, browseNotContentIndexedDirs = true, browseNotContentIndexedFiles = true, zipNotContentIndexedDirs = true, zipNotContentIndexedFiles = true, browseOfflineDirs = false, browseOfflineFiles = false, zipOfflineDirs = false, zipOfflineFiles = false, browseReadOnlyDirs = true, browseReadOnlyFiles = true, zipReadOnlyDirs = true, zipReadOnlyFiles = true, browseReparsePointDirs = true, browseReparsePointFiles = true, zipReparsePointDirs = true, zipReparsePointFiles = true, browseSparseFileDirs = false, browseSparseFileFiles = false, zipSparseFileDirs = false, zipSparseFileFiles = false, browseSystemDirs = false, browseSystemFiles = false, zipSystemDirs = false, zipSystemFiles = false, browseTemporaryDirs = false, browseTemporaryFiles = false, zipTemporaryDirs = false, zipTemporaryFiles = false;
		private string replace = "_";
		private int previewThreshold = 96;
		private ulong zipUploadThreshold = 40;

		public Settings (MainForm mainForm) {
			MainForm = mainForm;
			overwrite = RegistryUtil.GetBoolean ("Overwrite", overwrite);
			replace = RegistryUtil.GetString ("Replace", replace);
			browseArchiveDirs = RegistryUtil.GetBoolean ("BrowseArchiveDirs", browseArchiveDirs);
			browseArchiveFiles = RegistryUtil.GetBoolean ("BrowseArchiveFiles", browseArchiveFiles);
			suggestFiltersDocLibs = RegistryUtil.GetBoolean ("SuggestFiltersDocLibs", suggestFiltersDocLibs);
			suggestFiltersOtherLibs = RegistryUtil.GetBoolean ("SuggestFiltersOtherLibs", suggestFiltersOtherLibs);
			previewThreshold = RegistryUtil.GetInt32 ("PreviewThreshold", previewThreshold);
			ulong.TryParse (RegistryUtil.GetString ("ZipUploadThreshold", zipUploadThreshold.ToString ()), out zipUploadThreshold);
			zipArchiveDirs = RegistryUtil.GetBoolean ("ZipArchiveDirs", zipArchiveDirs);
			zipArchiveFiles = RegistryUtil.GetBoolean ("ZipArchiveFiles", zipArchiveFiles);
			browseCompressedDirs = RegistryUtil.GetBoolean ("BrowseCompressedDirs", browseCompressedDirs);
			browseCompressedFiles = RegistryUtil.GetBoolean ("BrowseCompressedFiles", browseCompressedFiles);
			zipCompressedDirs = RegistryUtil.GetBoolean ("ZipCompressedDirs", zipCompressedDirs);
			zipCompressedFiles = RegistryUtil.GetBoolean ("ZipCompressedFiles", zipCompressedFiles);
			browseEmptyDirs = RegistryUtil.GetBoolean ("BrowseEmptyDirs", browseEmptyDirs);
			browseEmptyFiles = RegistryUtil.GetBoolean ("BrowseEmptyFiles", browseEmptyFiles);
			zipEmptyDirs = RegistryUtil.GetBoolean ("ZipEmptyDirs", zipEmptyDirs);
			zipEmptyFiles = RegistryUtil.GetBoolean ("ZipEmptyFiles", zipEmptyFiles);
			browseEncryptedDirs = RegistryUtil.GetBoolean ("BrowseEncryptedDirs", browseEncryptedDirs);
			browseEncryptedFiles = RegistryUtil.GetBoolean ("BrowseEncryptedFiles", browseEncryptedFiles);
			zipEncryptedDirs = RegistryUtil.GetBoolean ("ZipEncryptedDirs", zipEncryptedDirs);
			zipEncryptedFiles = RegistryUtil.GetBoolean ("ZipEncryptedFiles", zipEncryptedFiles);
			browseErrorDirs = RegistryUtil.GetBoolean ("BrowseErrorDirs", browseErrorDirs);
			browseErrorFiles = RegistryUtil.GetBoolean ("BrowseErrorFiles", browseErrorFiles);
			zipErrorDirs = RegistryUtil.GetBoolean ("ZipErrorDirs", zipErrorDirs);
			zipErrorFiles = RegistryUtil.GetBoolean ("ZipErrorFiles", zipErrorFiles);
			browseBlockedFiles = RegistryUtil.GetBoolean ("BrowseBlockedFiles", browseBlockedFiles);
			zipBlockedFiles = RegistryUtil.GetBoolean ("ZipBlockedFiles", zipBlockedFiles);
			browseHiddenDirs = RegistryUtil.GetBoolean ("BrowseHiddenDirs", browseHiddenDirs);
			browseHiddenFiles = RegistryUtil.GetBoolean ("BrowseHiddenFiles", browseHiddenFiles);
			zipHiddenDirs = RegistryUtil.GetBoolean ("ZipHiddenDirs", zipHiddenDirs);
			zipHiddenFiles = RegistryUtil.GetBoolean ("ZipHiddenFiles", zipHiddenFiles);
			browseNotContentIndexedDirs = RegistryUtil.GetBoolean ("BrowseNotContentIndexedDirs", browseNotContentIndexedDirs);
			browseNotContentIndexedFiles = RegistryUtil.GetBoolean ("BrowseNotContentIndexedFiles", browseNotContentIndexedFiles);
			zipNotContentIndexedDirs = RegistryUtil.GetBoolean ("ZipNotContentIndexedDirs", zipNotContentIndexedDirs);
			zipNotContentIndexedFiles = RegistryUtil.GetBoolean ("ZipNotContentIndexedFiles", zipNotContentIndexedFiles);
			browseOfflineDirs = RegistryUtil.GetBoolean ("BrowseOfflineDirs", browseOfflineDirs);
			browseOfflineFiles = RegistryUtil.GetBoolean ("BrowseOfflineFiles", browseOfflineFiles);
			zipOfflineDirs = RegistryUtil.GetBoolean ("ZipOfflineDirs", zipOfflineDirs);
			zipOfflineFiles = RegistryUtil.GetBoolean ("ZipOfflineFiles", zipOfflineFiles);
			browseReadOnlyDirs = RegistryUtil.GetBoolean ("BrowseReadOnlyDirs", browseReadOnlyDirs);
			browseReadOnlyFiles = RegistryUtil.GetBoolean ("BrowseReadOnlyFiles", browseReadOnlyFiles);
			zipReadOnlyDirs = RegistryUtil.GetBoolean ("ZipReadOnlyDirs", zipReadOnlyDirs);
			zipReadOnlyFiles = RegistryUtil.GetBoolean ("ZipReadOnlyFiles", zipReadOnlyFiles);
			browseReparsePointDirs = RegistryUtil.GetBoolean ("BrowseReparsePointDirs", browseReparsePointDirs);
			browseReparsePointFiles = RegistryUtil.GetBoolean ("BrowseReparsePointFiles", browseReparsePointFiles);
			zipReparsePointDirs = RegistryUtil.GetBoolean ("ZipReparsePointDirs", zipReparsePointDirs);
			zipReparsePointFiles = RegistryUtil.GetBoolean ("ZipReparsePointFiles", zipReparsePointFiles);
			browseSparseFileDirs = RegistryUtil.GetBoolean ("BrowseSparseFileDirs", browseSparseFileDirs);
			browseSparseFileFiles = RegistryUtil.GetBoolean ("BrowseSparseFileFiles", browseSparseFileFiles);
			zipSparseFileDirs = RegistryUtil.GetBoolean ("ZipSparseFileDirs", zipSparseFileDirs);
			zipSparseFileFiles = RegistryUtil.GetBoolean ("ZipSparseFileFiles", zipSparseFileFiles);
			browseSystemDirs = RegistryUtil.GetBoolean ("BrowseSystemDirs", browseSystemDirs);
			browseSystemFiles = RegistryUtil.GetBoolean ("BrowseSystemFiles", browseSystemFiles);
			zipSystemDirs = RegistryUtil.GetBoolean ("ZipSystemDirs", zipSystemDirs);
			zipSystemFiles = RegistryUtil.GetBoolean ("ZipSystemFiles", zipSystemFiles);
			browseTemporaryDirs = RegistryUtil.GetBoolean ("BrowseTemporaryDirs", browseTemporaryDirs);
			browseTemporaryFiles = RegistryUtil.GetBoolean ("BrowseTemporaryFiles", browseTemporaryFiles);
			zipTemporaryDirs = RegistryUtil.GetBoolean ("ZipTemporaryDirs", zipTemporaryDirs);
			zipTemporaryFiles = RegistryUtil.GetBoolean ("ZipTemporaryFiles", zipTemporaryFiles);
		}

		public string GetCategory (CustomPropertyDescriptor property, object owner) {
			CategoryAttribute catAtt = null;
			foreach (Attribute att in property.Attributes)
				if ((catAtt = att as CategoryAttribute) != null)
					return res.ResourceManager.GetString ("Cat_" + catAtt.Category);
			return "Misc";
		}

		public string GetDescription (CustomPropertyDescriptor property, object owner) {
			bool isBrowse = false, isZip = false, isDirs = false, isFiles = false;
			if (((isBrowse = property.Name.StartsWith ("Browse")) || (isZip = property.Name.StartsWith ("Zip"))) && ((isFiles = property.Name.EndsWith ("Files")) || (isDirs = property.Name.EndsWith ("Dirs"))))
				return res.ResourceManager.GetString ("Desc_" + (isBrowse ? "Browse" : "Zip") + (isDirs ? "Dirs" : "Files"));
			return res.ResourceManager.GetString ("Desc_" + property.Name);
		}

		public string GetDisplayName (CustomPropertyDescriptor property, object owner) {
			bool isBrowse = false, isZip = false, isDirs = false, isFiles = false;
			int len;
			if (((isBrowse = property.Name.StartsWith ("Browse")) || (isZip = property.Name.StartsWith ("Zip"))) && ((isFiles = property.Name.EndsWith ("Files")) || (isDirs = property.Name.EndsWith ("Dirs"))))
				return res.ResourceManager.GetString ("FileAtt_" + property.Name.Substring (len = (isZip ? "Zip" : "Browse").Length, property.Name.Length - len - (isDirs ? "Dirs" : "Files").Length));
			return res.ResourceManager.GetString ("Title_" + property.Name);
		}

		public override PropertyDescriptorCollection GetProperties () {
			PropertyDescriptorCollection props = TypeDescriptor.GetProperties (this, true);
			List<PropertyDescriptor> col = new List<PropertyDescriptor> ();
			for (int i = 0; i < props.Count; i++)
				col.Add (new CustomPropertyDescriptor (props [i], this, this));
			return new PropertyDescriptorCollection (col.ToArray (), false);
		}

		public override PropertyDescriptorCollection GetProperties (Attribute [] attributes) {
			return GetProperties ();
		}

		public override object GetPropertyOwner (PropertyDescriptor pd) {
			return this;
		}

		[Category ("Upload"), DefaultValue (false)]
		public bool Overwrite {
			get {
				return overwrite;
			}
			set {
				RegistryUtil.SetBoolean ("Overwrite", overwrite = value);
			}
		}

		[Category ("Misc"), DefaultValue (96)]
		public int PreviewThreshold {
			get {
				return previewThreshold;
			}
			set {
				RegistryUtil.SetInt32 ("PreviewThreshold", previewThreshold = value);
			}
		}

		[Category ("Upload"), DefaultValue ("_")]
		public string Replace {
			get {
				return replace;
			}
			set {
				if (value == null)
					value = string.Empty;
				else
					foreach (char c in SPInvalidFileNameCharacters)
						if (value.IndexOf (c) >= 0)
							throw new Exception (res.Error_Replace);
				RegistryUtil.SetString ("Replace", replace = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("Misc"), DefaultValue (true)]
		public bool SuggestFiltersDocLibs {
			get {
				return suggestFiltersDocLibs;
			}
			set {
				RegistryUtil.SetBoolean ("SuggestFiltersDocLibs", suggestFiltersDocLibs = value);
			}
		}

		[Category ("Misc"), DefaultValue (true)]
		public bool SuggestFiltersOtherLibs {
			get {
				return suggestFiltersOtherLibs;
			}
			set {
				RegistryUtil.SetBoolean ("SuggestFiltersOtherLibs", suggestFiltersOtherLibs = value);
			}
		}

		[Category ("Upload"), DefaultValue ((ulong) 40)]
		public ulong ZipUploadThreshold {
			get {
				return zipUploadThreshold;
			}
			set {
				if ((value * 1024 * 1024) > (ComputerInfo.TotalPhysicalMemory / 4))
					value = ComputerInfo.TotalPhysicalMemory / 4 / 1024 / 1024;
				RegistryUtil.SetString ("ZipUploadThreshold", (zipUploadThreshold = value).ToString ());
			}
		}

		[Category ("BrowseDirs"), DefaultValue (false)]
		public bool BrowseErrorDirs {
			get {
				return browseErrorDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseErrorDirs", browseErrorDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseErrorFiles {
			get {
				return browseErrorFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseErrorFiles", browseErrorFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (false)]
		public bool ZipErrorDirs {
			get {
				return zipErrorDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipErrorDirs", zipErrorDirs = value);
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipErrorFiles {
			get {
				return zipErrorFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipErrorFiles", zipErrorFiles = value);
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseBlockedFiles {
			get {
				return browseBlockedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseBlockedFiles", browseBlockedFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipBlockedFiles {
			get {
				return zipBlockedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipBlockedFiles", zipBlockedFiles = value);
			}
		}

		[Category ("BrowseDirs"), DefaultValue (false)]
		public bool BrowseEmptyDirs {
			get {
				return browseEmptyDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseEmptyDirs", browseEmptyDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseEmptyFiles {
			get {
				return browseEmptyFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseEmptyFiles", browseEmptyFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (false)]
		public bool ZipEmptyDirs {
			get {
				return zipEmptyDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipEmptyDirs", zipEmptyDirs = value);
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipEmptyFiles {
			get {
				return zipEmptyFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipEmptyFiles", zipEmptyFiles = value);
			}
		}

		[Category ("BrowseDirs"), DefaultValue (true)]
		public bool BrowseArchiveDirs {
			get {
				return browseArchiveDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseArchiveDirs", browseArchiveDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (true)]
		public bool BrowseArchiveFiles {
			get {
				return browseArchiveFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseArchiveFiles", browseArchiveFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (true)]
		public bool ZipArchiveDirs {
			get {
				return zipArchiveDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipArchiveDirs", zipArchiveDirs = value);
			}
		}

		[Category ("ZipFiles"), DefaultValue (true)]
		public bool ZipArchiveFiles {
			get {
				return zipArchiveFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipArchiveFiles", zipArchiveFiles = value);
			}
		}

		[Category ("BrowseDirs"), DefaultValue (true)]
		public bool BrowseCompressedDirs {
			get {
				return browseCompressedDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseCompressedDirs", browseCompressedDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (true)]
		public bool BrowseCompressedFiles {
			get {
				return browseCompressedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseCompressedFiles", browseCompressedFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (true)]
		public bool ZipCompressedDirs {
			get {
				return zipCompressedDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipCompressedDirs", zipCompressedDirs = value);
			}
		}

		[Category ("ZipFiles"), DefaultValue (true)]
		public bool ZipCompressedFiles {
			get {
				return zipCompressedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipCompressedFiles", zipCompressedFiles = value);
			}
		}

		[Category ("BrowseDirs"), DefaultValue (true)]
		public bool BrowseEncryptedDirs {
			get {
				return browseEncryptedDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseEncryptedDirs", browseEncryptedDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (true)]
		public bool BrowseEncryptedFiles {
			get {
				return browseEncryptedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseEncryptedFiles", browseEncryptedFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (true)]
		public bool ZipEncryptedDirs {
			get {
				return zipEncryptedDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipEncryptedDirs", zipEncryptedDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (true)]
		public bool ZipEncryptedFiles {
			get {
				return zipEncryptedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipEncryptedFiles", zipEncryptedFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (false)]
		public bool BrowseHiddenDirs {
			get {
				return browseHiddenDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseHiddenDirs", browseHiddenDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseHiddenFiles {
			get {
				return browseHiddenFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseHiddenFiles", browseHiddenFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (false)]
		public bool ZipHiddenDirs {
			get {
				return zipHiddenDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipHiddenDirs", zipHiddenDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipHiddenFiles {
			get {
				return zipHiddenFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipHiddenFiles", zipHiddenFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (true)]
		public bool BrowseNotContentIndexedDirs {
			get {
				return browseNotContentIndexedDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseNotContentIndexedDirs", browseNotContentIndexedDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (true)]
		public bool BrowseNotContentIndexedFiles {
			get {
				return browseNotContentIndexedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseNotContentIndexedFiles", browseNotContentIndexedFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (true)]
		public bool ZipNotContentIndexedDirs {
			get {
				return zipNotContentIndexedDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipNotContentIndexedDirs", zipNotContentIndexedDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (true)]
		public bool ZipNotContentIndexedFiles {
			get {
				return zipNotContentIndexedFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipNotContentIndexedFiles", zipNotContentIndexedFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (false)]
		public bool BrowseOfflineDirs {
			get {
				return browseOfflineDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseOfflineDirs", browseOfflineDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseOfflineFiles {
			get {
				return browseOfflineFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseOfflineFiles", browseOfflineFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (false)]
		public bool ZipOfflineDirs {
			get {
				return zipOfflineDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipOfflineDirs", zipOfflineDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipOfflineFiles {
			get {
				return zipOfflineFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipOfflineFiles", zipOfflineFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (true)]
		public bool BrowseReadOnlyDirs {
			get {
				return browseReadOnlyDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseReadOnlyDirs", browseReadOnlyDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (true)]
		public bool BrowseReadOnlyFiles {
			get {
				return browseReadOnlyFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseReadOnlyFiles", browseReadOnlyFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (true)]
		public bool ZipReadOnlyDirs {
			get {
				return zipReadOnlyDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipReadOnlyDirs", zipReadOnlyDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (true)]
		public bool ZipReadOnlyFiles {
			get {
				return zipReadOnlyFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipReadOnlyFiles", zipReadOnlyFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (true)]
		public bool BrowseReparsePointDirs {
			get {
				return browseReparsePointDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseReparsePointDirs", browseReparsePointDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (true)]
		public bool BrowseReparsePointFiles {
			get {
				return browseReparsePointFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseReparsePointFiles", browseReparsePointFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (true)]
		public bool ZipReparsePointDirs {
			get {
				return zipReparsePointDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipReparsePointDirs", zipReparsePointDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (true)]
		public bool ZipReparsePointFiles {
			get {
				return zipReparsePointFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipReparsePointFiles", zipReparsePointFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (false)]
		public bool BrowseSparseFileDirs {
			get {
				return browseSparseFileDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseSparseFileDirs", browseSparseFileDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseSparseFileFiles {
			get {
				return browseSparseFileFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseSparseFileFiles", browseSparseFileFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (false)]
		public bool ZipSparseFileDirs {
			get {
				return zipSparseFileDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipSparseFileDirs", zipSparseFileDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipSparseFileFiles {
			get {
				return zipSparseFileFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipSparseFileFiles", zipSparseFileFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (false)]
		public bool BrowseSystemDirs {
			get {
				return browseSystemDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseSystemDirs", browseSystemDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseSystemFiles {
			get {
				return browseSystemFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseSystemFiles", browseSystemFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (false)]
		public bool ZipSystemDirs {
			get {
				return zipSystemDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipSystemDirs", zipSystemDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipSystemFiles {
			get {
				return zipSystemFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipSystemFiles", zipSystemFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseDirs"), DefaultValue (false)]
		public bool BrowseTemporaryDirs {
			get {
				return browseTemporaryDirs;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseTemporaryDirs", browseTemporaryDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("BrowseFiles"), DefaultValue (false)]
		public bool BrowseTemporaryFiles {
			get {
				return browseTemporaryFiles;
			}
			set {
				RegistryUtil.SetBoolean ("BrowseTemporaryFiles", browseTemporaryFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipDirs"), DefaultValue (false)]
		public bool ZipTemporaryDirs {
			get {
				return zipTemporaryDirs;
			}
			set {
				RegistryUtil.SetBoolean ("ZipTemporaryDirs", zipTemporaryDirs = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

		[Category ("ZipFiles"), DefaultValue (false)]
		public bool ZipTemporaryFiles {
			get {
				return zipTemporaryFiles;
			}
			set {
				RegistryUtil.SetBoolean ("ZipTemporaryFiles", zipTemporaryFiles = value);
				MainForm.multiExplorerControl.RefreshBrowseListView ();
			}
		}

	}

	#endregion

#region Uploader Class

	public class Uploader : IDisposable {

		private readonly WebClientEx wc = null;
		private readonly Files up = null;
		private readonly Dictionary<UploadDocumentCompletedEventHandler, UploadDataCompletedEventHandler> handlers = new Dictionary<UploadDocumentCompletedEventHandler, UploadDataCompletedEventHandler> ();

		private string url = string.Empty;

		public Uploader (bool ws) {
			if (ws)
				up = new Files ();
			else
				wc = new WebClientEx ();
		}

		public void Abort () {
			if (up != null)
				up.Abort ();
			else if (wc.reqs.ContainsKey (url))
				try {
					wc.reqs [url].Abort ();
				} catch {
				}
		}

		public void Add (UploadDocumentCompletedEventHandler onComplete) {
			if (up != null)
				up.UploadDocumentCompleted += onComplete;
			else if (!handlers.ContainsKey (onComplete))
				wc.UploadDataCompleted += (handlers [onComplete] = delegate (object sender, UploadDataCompletedEventArgs e) {
					object [] result = new object [0];
					string msg;
					Exception error = e.Error;
					try {
						result = new object [] { msg = Encoding.Default.GetString (e.Result) };
						if (msg.StartsWith ("roxupex::", StringComparison.InvariantCultureIgnoreCase)) {
							error = new Exception (msg.Substring ("roxupex::".Length));
							msg = string.Empty;
						}
					} catch {
					}
					onComplete (sender, new UploadDocumentCompletedEventArgs (result, error, e.Cancelled, e.UserState));
				});
		}

		public void CancelAsync (object userState) {
			if (up != null)
				up.CancelAsync (userState);
			else
				wc.CancelAsync ();
		}

		public void Dispose () {
			((up == null) ? wc : (IDisposable) up).Dispose ();
		}

		public void Remove (UploadDocumentCompletedEventHandler onComplete) {
			if (up != null)
				up.UploadDocumentCompleted -= onComplete;
			else if (handlers.ContainsKey (onComplete)) {
				wc.UploadDataCompleted -= (handlers [onComplete]);
				handlers.Remove (onComplete);
			}
		}

		public void UploadDocumentAsync (string fileName, byte [] fileData, string folderName, bool unzip, bool overwrite, string autoCheckIn, bool autoCheckOut, string replace, byte [] metaDataRaw, bool hasHelp, object userState) {
			Converter<string, string> e = HttpUtility.UrlEncode;
			Converter<bool, string> b = delegate (bool v) {
				return v ? "1" : "0";
			};
			if (up != null)
				up.UploadDocumentAsync (fileName, fileData, folderName, unzip, overwrite, autoCheckIn, autoCheckOut, replace, metaDataRaw, hasHelp, userState);
			else
				wc.UploadDataAsync (new Uri (url.Replace ("/Files.asmx", string.Format ("/uploadzen.aspx?fn={0}&dn={1}&uz={2}&ow={3}&ci={4}&co={5}&rp={6}&hh={7}", e (fileName), e (folderName), b (unzip), b (overwrite), e (autoCheckIn), b (autoCheckOut), e (replace), b (hasHelp)))), null, fileData, userState);
		}

		public CookieContainer CookieContainer {
			get {
				return ((up != null) ? up.CookieContainer : wc.CookieContainer);
			}
			set {
				if (up != null)
					up.CookieContainer = value;
				else
					wc.CookieContainer = value;
			}
		}

		public ICredentials Credentials {
			get {
				return ((up != null) ? up.Credentials : wc.Credentials);
			}
			set {
				//NetworkCredential netcred;
				//try {
				//    if ((value != null) && (((netcred = value as NetworkCredential) != null) || ((netcred = value.GetCredential (new Uri (url), "ntlm")) != null) || ((netcred = value.GetCredential (new Uri (url), null)) != null)) && string.IsNullOrEmpty (netcred.UserName))
				//        value = null;
				//} catch {
				//}
				if (up != null) {
					if (value != null) {
						up.UseDefaultCredentials = false;
						up.Credentials = value;
					} else {
						up.Credentials = null;
						up.UseDefaultCredentials = true;
					}
				} else
					if (value != null) {
						wc.UseDefaultCredentials = false;
						wc.Credentials = value;
					} else {
						wc.Credentials = null;
						wc.UseDefaultCredentials = true;
					}
			}
		}

		public bool PreAuthenticate {
			get {
				return ((up != null) ? up.PreAuthenticate : wc.PreAuthenticate);
			}
			set {
				if (up != null)
					up.PreAuthenticate = value;
				else
					wc.PreAuthenticate = value;
			}
		}

		public string Url {
			get {
				return url;
			}
			set {
				url = value;
				if (up != null)
					up.Url = value;
			}
		}

	}

#endregion

#region WebClientEx Class

	public class WebClientEx : WebClient {

		internal Dictionary<string, WebRequest> reqs = new Dictionary<string, WebRequest> ();

		protected override WebRequest GetWebRequest (Uri address) {
			WebRequest req = base.GetWebRequest (address);
			HttpWebRequest http = req as HttpWebRequest;
			reqs [address.ToString ()] = req;
			if (http != null) {
				http.CookieContainer = CookieContainer;
				http.PreAuthenticate = PreAuthenticate;
			}
			return req;
		}

		public CookieContainer CookieContainer {
			get;
			set;
		}

		public bool PreAuthenticate {
			get;
			set;
		}

	}

#endregion

}
