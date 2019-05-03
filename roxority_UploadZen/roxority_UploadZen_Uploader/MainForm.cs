
using roxority.Shared.IO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Deployment.Application;
using System.Drawing;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Services.Protocols;
using System.Windows.Forms;
using System.Xml;

using WssAuth = roxUp.WssAuthentication;

namespace roxUp {

	using res = Properties.Resources;

	public partial class MainForm : Form {

		public static readonly int [] docLibTemplates = { 101, 109, 110, 111, 113, 114, 115, 116, 119, 130, 212, 2002, 2003 };
		public static readonly string [] suppressedMetaNames = { "ID" };
		public static readonly string [] suppressedMetaTypes = { "ContentTypeId", "Computed", "Lookup", "ModStat" };

		public readonly Settings Settings;

		internal int cookieAttempts = 0;
		internal string lastListName = string.Empty, siteTitle = string.Empty;

		public MainForm (string [] args) {
			InitializeComponent ();
			propertyGrid.PropertySort = PropertySort.Categorized;
			propertyGrid.SelectedObject = Settings = new Settings (this);
			propertyGrid.CollapseAllGridItems ();
			if (args != null) {
				if (args.Length > 0)
					multiExplorerControl.Url = args [0];
				if (args.Length > 1)
					multiExplorerControl.ListName = args [1];
				if (args.Length > 2)
					multiExplorerControl.FolderName = args [2];
			}
		}

		private void folderMenuItem_Click (object sender, EventArgs e) {
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			statusFolderItem.DropDown.Close (ToolStripDropDownCloseReason.ItemClicked);
			multiExplorerControl.FolderName = item.Tag + "";
		}

		private void listMenuItem_Click (object sender, EventArgs e) {
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			multiExplorerControl.folderName = string.Empty;
			multiExplorerControl.ListName = item.Tag + "";
		}

		private void multiExplorerControl_PropertyChanged (object sender, EventArgs e) {
			XmlNode listCol = null;
			string [] folderPath;
			ToolStripMenuItem folderItem;
			WssLists.Lists lists = new roxUp.WssLists.Lists ();
			WssSiteData.SiteData siteData = new roxUp.WssSiteData.SiteData ();
			WssLists.GetListCollectionCompletedEventHandler onLists = null;
			WssSiteData.EnumerateFolderCompletedEventHandler onFolders = null;
			onFolders = delegate (object tmp2, WssSiteData.EnumerateFolderCompletedEventArgs folderArgs) {
				WebException webEx = folderArgs.Error as WebException;
				HttpWebResponse resp;
				Cookie cookie;
				CookieCollection cookies;
				DialogResult prompt;
				WssAuth.LoginResult authResult;
				int pos;
				string userState = folderArgs.UserState as string, pathPrefix = string.Empty;
				ToolStripItemCollection items = statusFolderItem.DropDownItems;
				List<WssSiteData._sFPUrl> folders = new List<roxUp.WssSiteData._sFPUrl> ();
				statusFolderItem.Enabled = multiExplorerControl.allowFolderChange && !multiExplorerControl.isUploadBusy;
				if ((!string.IsNullOrEmpty (userState)) && ((folderPath = userState.Split (new char [] { '/' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (folderPath.Length > 0))
					for (int i = 0; i < folderPath.Length; i++)
						foreach (ToolStripItem item in items)
							if (((folderItem = item as ToolStripMenuItem) != null) && (!string.IsNullOrEmpty (item.Tag + "")) && item.Tag.ToString ().Equals (pathPrefix + folderPath [i])) {
								folderItem.Checked = multiExplorerControl.FolderName.StartsWith (item.Tag.ToString ());
								items = folderItem.DropDownItems;
								folderItem.Enabled = true;
								pathPrefix = pathPrefix + folderPath [i] + '/';
								break;
							}
				if (items != statusFolderItem.DropDownItems)
					items.Clear ();
				else
					while (items.Count > 2)
						items.RemoveAt (2);
				if ((webEx != null) && ((resp = webEx.Response as HttpWebResponse) != null) && ((resp.StatusCode == HttpStatusCode.Unauthorized) || (resp.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) || (resp.StatusCode == HttpStatusCode.Forbidden))) {
					multiExplorerControl.lastCookie = null;
					using (LoginForm loginForm = new LoginForm (multiExplorerControl.allowSavePassword, multiExplorerControl.isUserLoginStored)) {
						if ((!multiExplorerControl.isFormsAuth) && string.IsNullOrEmpty (multiExplorerControl.logonPass) && string.IsNullOrEmpty (multiExplorerControl.logonDomain) && string.IsNullOrEmpty (multiExplorerControl.logonUser)) {
							multiExplorerControl.logonDomain = Environment.UserDomainName;
							multiExplorerControl.logonUser = Environment.UserName;
						}
						loginForm.userNameTextBox.Text = (string.IsNullOrEmpty (multiExplorerControl.logonDomain) ? multiExplorerControl.logonUser : (multiExplorerControl.logonDomain + '\\' + multiExplorerControl.logonUser));
						loginForm.passwordTextBox.Text = multiExplorerControl.logonPass;
						prompt = ((multiExplorerControl.isFormsAuth && multiExplorerControl.firstAttempt && (!string.IsNullOrEmpty (multiExplorerControl.logonPass)) && (!string.IsNullOrEmpty (multiExplorerControl.logonUser))) ? DialogResult.OK : loginForm.ShowDialog (this));
						multiExplorerControl.firstAttempt = false;
						if (prompt == DialogResult.OK) {
							multiExplorerControl.logonDomain = (((pos = loginForm.userNameTextBox.Text.IndexOf ('\\')) > 0) ? loginForm.userNameTextBox.Text.Substring (0, pos) : string.Empty);
							multiExplorerControl.logonUser = loginForm.userNameTextBox.Text.Substring ((pos >= 0) ? (pos + 1) : 0);
							multiExplorerControl.logonPass = loginForm.passwordTextBox.Text;
							multiExplorerControl.SaveOrForgetUserData (loginForm.checkBox.Checked);
							if (multiExplorerControl.isFormsAuth) {
								using (WssAuth.Authentication auth = new WssAuth.Authentication ())
									try {
										auth.Url = multiExplorerControl.Url.Replace ("_layouts/roxority_UploadZen/Files.asmx", "_vti_bin/Authentication.asmx");
										auth.CookieContainer = new CookieContainer ();
										if (((cookie = multiExplorerControl.lastCookie) != null) || (((authResult = auth.Login (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass)) != null) && (authResult.ErrorCode == WssAuth.LoginErrorCode.NoError) && (!string.IsNullOrEmpty (authResult.CookieName)) && ((cookies = auth.CookieContainer.GetCookies (new Uri (auth.Url))) != null) && ((cookie = cookies [authResult.CookieName]) != null))) {
											multiExplorerControl.lastCookie = cookie;
											EnsureCookie (siteData);
										} else {
											multiExplorerControl.lastCookie = null;
											if (authResult == null)
												throw new UnauthorizedAccessException ();
											else if (authResult.ErrorCode == roxUp.WssAuthentication.LoginErrorCode.NotInFormsAuthenticationMode) {
												multiExplorerControl.isFormsAuth = false;
												lists.PreAuthenticate = siteData.PreAuthenticate = true;
												lists.Credentials = siteData.Credentials = ((string.IsNullOrEmpty (multiExplorerControl.LogonUser) && !multiExplorerControl.forceLogin) ? (CredentialCache.DefaultNetworkCredentials) : (string.IsNullOrEmpty (multiExplorerControl.LogonDomain) ? new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass) : new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass, multiExplorerControl.LogonDomain)));
											} else if (authResult.ErrorCode != roxUp.WssAuthentication.LoginErrorCode.NoError)
												throw new UnauthorizedAccessException (res.AuthError_PasswordNotMatch);
										}
									} catch (Exception ex) {
										multiExplorerControl.lastCookie = null;
										MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
									}
							} else {
								lists.PreAuthenticate = siteData.PreAuthenticate = true;
								lists.Credentials = siteData.Credentials = ((string.IsNullOrEmpty (multiExplorerControl.LogonUser) && !multiExplorerControl.forceLogin) ? (CredentialCache.DefaultNetworkCredentials) : (string.IsNullOrEmpty (multiExplorerControl.LogonDomain) ? new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass) : new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass, multiExplorerControl.LogonDomain)));
							}
							siteData.EnumerateFolderAsync (folderArgs.UserState.ToString ());
						} else {
							siteData.EnumerateFolderCompleted -= onFolders;
							siteData.Dispose ();
							siteData = null;
						}
					}
					multiExplorerControl.UpdateUserItem ();
				} else if (folderArgs.Error != null) {
					siteData.EnumerateFolderCompleted -= onFolders;
					siteData.Dispose ();
					siteData = null;
					if (MessageBox.Show (this, folderArgs.Error.Message, Application.ProductName, (lists != null) ? MessageBoxButtons.RetryCancel : MessageBoxButtons.OK, MessageBoxIcon.Warning) == DialogResult.Retry) {
						lists.GetListCollectionCompleted += onLists;
						lists.GetListCollectionAsync ();
					}
				} else if ((folderArgs.Result == 0) && (folderArgs.vUrls != null) && (folderArgs.vUrls.Length > 0)) {
					folders.AddRange (folderArgs.vUrls);
					folders.RemoveAll (delegate (WssSiteData._sFPUrl sfpUrl) {
						return ((!sfpUrl.IsFolder) || sfpUrl.Url.Substring (sfpUrl.Url.IndexOf ('/') + 1).Equals ("Forms"));
					});
					folders.Sort (delegate (WssSiteData._sFPUrl one, WssSiteData._sFPUrl two) {
						return one.Url.CompareTo (two.Url);
					});
					foreach (WssSiteData._sFPUrl sfpUrl in folders) {
						folderItem = items.Add (sfpUrl.Url.Substring (sfpUrl.Url.LastIndexOf ('/') + 1), statusFolderItem.Image, folderMenuItem_Click) as ToolStripMenuItem;
						folderItem.Enabled = false;
						folderItem.Tag = sfpUrl.Url.Substring (sfpUrl.Url.IndexOf ('/') + 1);
						siteData.EnumerateFolderAsync (sfpUrl.Url, folderItem.Tag + "");
					}
				}
				statusFolderItem.DropDownItems [1].Visible = ((folders.Count > 0) || (!string.IsNullOrEmpty (pathPrefix)));
			};
			onLists = delegate (object tmp, WssLists.GetListCollectionCompletedEventArgs args) {
				WebException webEx = args.Error as WebException;
				HttpWebResponse resp;
				ToolStripMenuItem item;
				Cookie cookie;
				CookieCollection cookies;
				DialogResult prompt;
				WssSiteData._sListMetadata listMetaData = null;
				WssSiteData._sProperty [] listProps = null;
				WssAuth.LoginResult authResult;
				int pos, listTemp;
				uint metaResult;
				if ((webEx != null) && ((resp = webEx.Response as HttpWebResponse) != null) && ((resp.StatusCode == HttpStatusCode.Unauthorized) || (resp.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) || (resp.StatusCode == HttpStatusCode.Forbidden))) {
					multiExplorerControl.lastCookie = null;
					using (LoginForm loginForm = new LoginForm (multiExplorerControl.allowSavePassword, multiExplorerControl.isUserLoginStored)) {
						if ((!multiExplorerControl.isFormsAuth) && string.IsNullOrEmpty (multiExplorerControl.logonPass) && string.IsNullOrEmpty (multiExplorerControl.logonDomain) && string.IsNullOrEmpty (multiExplorerControl.logonUser)) {
							multiExplorerControl.logonDomain = Environment.UserDomainName;
							multiExplorerControl.logonUser = Environment.UserName;
						}
						loginForm.userNameTextBox.Text = (string.IsNullOrEmpty (multiExplorerControl.logonDomain) ? multiExplorerControl.logonUser : (multiExplorerControl.logonDomain + '\\' + multiExplorerControl.logonUser));
						loginForm.passwordTextBox.Text = multiExplorerControl.logonPass;
						prompt = ((multiExplorerControl.isFormsAuth && multiExplorerControl.firstAttempt && (!string.IsNullOrEmpty (multiExplorerControl.logonPass)) && (!string.IsNullOrEmpty (multiExplorerControl.logonUser))) ? DialogResult.OK : loginForm.ShowDialog (this));
						multiExplorerControl.firstAttempt = false;
						if (prompt == DialogResult.OK) {
							multiExplorerControl.logonDomain = (((pos = loginForm.userNameTextBox.Text.IndexOf ('\\')) > 0) ? loginForm.userNameTextBox.Text.Substring (0, pos) : string.Empty);
							multiExplorerControl.logonUser = loginForm.userNameTextBox.Text.Substring ((pos >= 0) ? (pos + 1) : 0);
							multiExplorerControl.logonPass = loginForm.passwordTextBox.Text;
							multiExplorerControl.SaveOrForgetUserData (loginForm.checkBox.Checked);
							if (multiExplorerControl.isFormsAuth) {
								using (WssAuth.Authentication auth = new WssAuth.Authentication ())
									try {
										auth.Url = multiExplorerControl.Url.Replace ("_layouts/roxority_UploadZen/Files.asmx", "_vti_bin/Authentication.asmx");
										auth.CookieContainer = new CookieContainer ();
										if (((cookie = multiExplorerControl.lastCookie) != null) || (((authResult = auth.Login (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass)) != null) && (authResult.ErrorCode == WssAuth.LoginErrorCode.NoError) && (!string.IsNullOrEmpty (authResult.CookieName)) && ((cookies = auth.CookieContainer.GetCookies (new Uri (auth.Url))) != null) && ((cookie = cookies [authResult.CookieName]) != null))) {
											multiExplorerControl.lastCookie = cookie;
											EnsureCookie (lists, siteData);
										} else {
											multiExplorerControl.lastCookie = null;
											if (authResult == null)
												throw new UnauthorizedAccessException ();
											else if (authResult.ErrorCode == roxUp.WssAuthentication.LoginErrorCode.NotInFormsAuthenticationMode) {
												multiExplorerControl.isFormsAuth = false;
												lists.PreAuthenticate = siteData.PreAuthenticate = true;
												lists.Credentials = siteData.Credentials = ((string.IsNullOrEmpty (multiExplorerControl.LogonUser) && !multiExplorerControl.forceLogin) ? (CredentialCache.DefaultNetworkCredentials) : (string.IsNullOrEmpty (multiExplorerControl.LogonDomain) ? new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass) : new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass, multiExplorerControl.LogonDomain)));
											} else if (authResult.ErrorCode != roxUp.WssAuthentication.LoginErrorCode.NoError)
												throw new UnauthorizedAccessException (res.AuthError_PasswordNotMatch);
										}
									} catch (Exception ex) {
										multiExplorerControl.lastCookie = null;
										MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
									}
							} else {
								siteData.PreAuthenticate = lists.PreAuthenticate = true;
								siteData.Credentials = lists.Credentials = ((string.IsNullOrEmpty (multiExplorerControl.LogonUser) && !multiExplorerControl.forceLogin) ? (CredentialCache.DefaultNetworkCredentials) : (string.IsNullOrEmpty (multiExplorerControl.LogonDomain) ? new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass) : new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass, multiExplorerControl.LogonDomain)));
							}
							lists.GetListCollectionAsync ();
						} else {
							lists.GetListCollectionCompleted -= onLists;
							lists.Dispose ();
							lists = null;
						}
					}
					multiExplorerControl.UpdateUserItem ();
				} else if (args.Error != null) {
					if (MessageBox.Show (this, args.Error.Message, Application.ProductName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry)
						lists.GetListCollectionAsync ();
					else {
						lists.GetListCollectionCompleted -= onLists;
						lists.Dispose ();
						lists = null;
					}
				} else {
					try {
						listCol = args.Result;
					} catch {
						listCol = null;
					} finally {
						lists.GetListCollectionCompleted -= onLists;
						lists.Dispose ();
						lists = null;
					}
					if (listCol != null) {
						statusListItem.DropDownItems.Clear ();
						foreach (XmlNode listNode in listCol.ChildNodes)
							if (!string.IsNullOrEmpty (listNode.Attributes ["DefaultViewUrl"].Value) && ((Array.IndexOf<int> (docLibTemplates, listTemp = int.Parse (listNode.Attributes ["ServerTemplate"].Value)) >= 0) || !string.IsNullOrEmpty (listNode.Attributes ["DocTemplateUrl"].Value))) {
								item = statusListItem.DropDownItems.Add (listNode.Attributes ["Title"].Value, null, listMenuItem_Click) as ToolStripMenuItem;
								item.Name = listNode.Attributes ["ID"].Value;
								item.Tag = listNode.Attributes ["DefaultViewUrl"].Value.Substring (listNode.Attributes ["WebFullUrl"].Value.Length);
								if (item.Tag.ToString ().Contains ("/") && ((item.Tag = item.Tag.ToString ().Substring (0, item.Tag.ToString ().LastIndexOf ('/')).Trim ('/')).ToString ().EndsWith ("/Forms")))
									item.Tag = item.Tag.ToString ().Substring (0, item.Tag.ToString ().LastIndexOf ('/'));
								if (item.Checked = ((statusSiteItem.ToolTipText + '/' + listNode.Attributes ["DefaultViewUrl"].Value.Substring (listNode.Attributes ["WebFullUrl"].Value.Length).Trim ('/')).ToLowerInvariant ().StartsWith ((statusSiteItem.ToolTipText + '/' + multiExplorerControl.ListName).ToLowerInvariant ()))) {
									if ((statusListItem.Text = item.Text).Equals (statusListItem.ToolTipText = (statusListItem.Tag = item.Tag) + ""))
										statusListItem.ToolTipText = string.Empty;
									statusListItem.Name = item.Name;
									multiExplorerControl.SetFilterByTemplate (listNode.Attributes ["ServerTemplate"].Value);
								}
							}
						statusFolderItem.DropDown.Close (ToolStripDropDownCloseReason.CloseCalled);
						if (multiExplorerControl.allowFolderChange) {
							siteData.EnumerateFolderCompleted += onFolders;
							siteData.EnumerateFolderAsync (statusListItem.Tag + "", string.Empty);
						}
						if (lastListName != multiExplorerControl.ListName) {
							multiExplorerControl.propertyGrid.SelectedObject = null;
							multiExplorerControl.metaListView.Items.Clear ();
							listMetaData = null;
							listProps = null;
							lastListName = multiExplorerControl.ListName;
							Application.DoEvents ();
							try {
								metaResult = siteData.GetList (statusListItem.Name, out listMetaData, out listProps);
							} catch (Exception ex) {
								SoapException soapEx = ex as SoapException;
								MessageBox.Show (this, ex.Message + (((soapEx == null) || (soapEx.Detail == null)) ? string.Empty : ("\r\n\r\n" + soapEx.Detail.OuterXml)), ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							}
							Application.DoEvents ();
							if (listProps != null)
								foreach (WssSiteData._sProperty prop in listProps)
									if ((Array.IndexOf<string> (suppressedMetaNames, prop.Name) < 0) && (Array.IndexOf<string> (suppressedMetaTypes, prop.Type) < 0))
										multiExplorerControl.metaListView.Items.Add (new MetaDataWrap (prop).ToListViewItem ());
							multiExplorerControl.metaListView.AutoResizeColumns ((multiExplorerControl.metaListView.Items.Count > 0) ? ColumnHeaderAutoResizeStyle.ColumnContent : ColumnHeaderAutoResizeStyle.HeaderSize);
							multiExplorerControl.metaListView.AutoResizeColumn (2, ColumnHeaderAutoResizeStyle.HeaderSize);
						}
					}
					statusListItem.Enabled = multiExplorerControl.allowListChange && !multiExplorerControl.isUploadBusy;
				}
			};
			statusFolderItem.Enabled = statusListItem.Enabled = false;
			statusListItem.DropDownItems.Clear ();
			statusFolderItem.DropDownItems.Clear ();
			statusFolderItem.DropDownItems.Add (folderItem = new ToolStripMenuItem (res.Root, statusFolderItem.Image, folderMenuItem_Click));
			folderItem.Checked = string.IsNullOrEmpty (multiExplorerControl.FolderName);
			folderItem.Tag = string.Empty;
			statusFolderItem.DropDownItems.Add (new ToolStripSeparator ());
			statusListItem.Text = multiExplorerControl.ListName;
			statusFolderItem.Text = string.IsNullOrEmpty (multiExplorerControl.FolderName) ? res.Root : multiExplorerControl.FolderName;
			statusSiteItem.ToolTipText = multiExplorerControl.Url.Replace ("/_layouts/roxority_UploadZen/Files.asmx", string.Empty);
			statusSiteItem.Text = (string.IsNullOrEmpty (siteTitle) ? statusSiteItem.ToolTipText.Substring (statusSiteItem.ToolTipText.IndexOf ("://") + "://".Length) : siteTitle);
			lists.Url = statusSiteItem.ToolTipText.TrimEnd ('/') + "/_vti_bin/Lists.asmx";
			siteData.Url = statusSiteItem.ToolTipText.TrimEnd ('/') + "/_vti_bin/SiteData.asmx";
			if (!multiExplorerControl.isFormsAuth) {
				siteData.PreAuthenticate = lists.PreAuthenticate = true;
				siteData.Credentials = lists.Credentials = ((string.IsNullOrEmpty (multiExplorerControl.LogonUser) && !multiExplorerControl.forceLogin) ? CredentialCache.DefaultNetworkCredentials : (string.IsNullOrEmpty (multiExplorerControl.LogonDomain) ? new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass) : new NetworkCredential (multiExplorerControl.LogonUser, multiExplorerControl.LogonPass, multiExplorerControl.LogonDomain)));
			} else
				EnsureCookie (lists, siteData);
			if (multiExplorerControl.allowListChange || multiExplorerControl.allowFolderChange) {
				lists.GetListCollectionCompleted += onLists;
				lists.GetListCollectionAsync ();
			}
			multiExplorerControl.UpdateUserItem ();
		}

		private void multiExplorerControl_UploadBusy (object sender, EventArgs e) {
			statusListItem.Enabled = statusFolderItem.Enabled = false;
		}

		private void multiExplorerControl_UploadNonBusy (object sender, EventArgs e) {
			statusListItem.Enabled = multiExplorerControl.allowListChange;
			statusFolderItem.Enabled = multiExplorerControl.allowFolderChange;
		}

		private void statusHelpLabel_Click (object sender, EventArgs e) {
			ShellInfo.ExecuteVerb (new ShellInfo.Verb (string.Empty, "http://roxority.com/UploadZen/", null), Handle, true, string.Empty);
		}

		private void statusLinkLabel_Click (object sender, EventArgs e) {
			if (sender == statusSiteItem)
				ShellInfo.ExecuteVerb (new ShellInfo.Verb (string.Empty, statusSiteItem.ToolTipText, null), Handle, true, string.Empty);
			else if (sender == statusHelpLinkLabel)
				ShellInfo.ExecuteVerb (new ShellInfo.Verb (string.Empty, statusSiteItem.ToolTipText.TrimEnd ('/') + "/_layouts/roxority_UploadZen/default.aspx?doc=" + ((tabControl.SelectedTab == prepTabPage) ? "client_side_settings" : ((tabControl.SelectedTab == uploadTabPage) ? "uploading_multiple_files#s1" : "bulk_folder_uploads_server_side_zip_unzipping")), null), Handle, true, string.Empty);
			else
				ShellInfo.ExecuteVerb (new ShellInfo.Verb (string.Empty, statusSiteItem.ToolTipText + "/" + statusListItem.Text, null), Handle, true, string.Empty);
		}

		internal void EnsureCookie (params HttpWebClientProtocol [] webServices) {
			if (multiExplorerControl.lastCookie != null)
				foreach (HttpWebClientProtocol ws in webServices) {
					ws.CookieContainer = new CookieContainer ();
					ws.CookieContainer.Add (multiExplorerControl.lastCookie);
				}
		}

		internal NameValueCollection GetQueryStringParameters () {
			NameValueCollection nameValueTable = new NameValueCollection ();
			string queryString;
			try {
				if (ApplicationDeployment.IsNetworkDeployed) {
					queryString = ApplicationDeployment.CurrentDeployment.ActivationUri.Query;
					nameValueTable = HttpUtility.ParseQueryString (queryString);
				}
			} catch {
			}
			return (nameValueTable);
		}

		protected override void OnLoad (EventArgs e) {
			NameValueCollection urlParams;
			string value, logonUser, logonDomain;
			int pos;
			base.OnLoad (e);
			if (((urlParams = GetQueryStringParameters ()) != null) && (urlParams.Count > 0)) {
				if (!"1".Equals (urlParams ["c"]))
					tabControl.TabPages.Remove (prepTabPage);
				multiExplorerControl.allowFolderChange = "1".Equals (urlParams ["cf"]);
				multiExplorerControl.allowListChange = "1".Equals (urlParams ["cl"]);
				multiExplorerControl.allowSavePassword = !"1".Equals (urlParams ["d"]);
				if (!string.IsNullOrEmpty (urlParams ["be"])) {
					multiExplorerControl.blockedExtensions.Clear ();
					multiExplorerControl.blockedExtensions.AddRange (urlParams ["be"].ToLowerInvariant ().Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
				}
				if (((multiExplorerControl.li = urlParams ["li"]) == "b") || (multiExplorerControl.li == "u")) {
					statusHelpLinkLabel.Visible = !(multiExplorerControl.hideHelp = "1".Equals (urlParams ["hh"]));
					statusHelpLabel.Visible = !(multiExplorerControl.hideLink = "1".Equals (urlParams ["hl"]));
					multiExplorerControl.hideTutorial = "1".Equals (urlParams ["ht"]);
					multiExplorerControl.hideNote = (!"0".Equals (urlParams ["hn"]));
				}
				multiExplorerControl.isFormsAuth = "0".Equals (urlParams ["w"]);
				multiExplorerControl.zipHandling = urlParams ["z"];
				if (!(string.IsNullOrEmpty (value = urlParams ["s"])))
					multiExplorerControl.url = value + "/_layouts/roxority_UploadZen/Files.asmx";
				if (!(string.IsNullOrEmpty (value = urlParams ["f"])))
					multiExplorerControl.folderName = value;
				if (!(string.IsNullOrEmpty (value = urlParams ["l"])))
					multiExplorerControl.listName = value;
				if (!(string.IsNullOrEmpty (value = urlParams ["t"])))
					siteTitle = value;
				if (string.IsNullOrEmpty (multiExplorerControl.LogonUser) && !string.IsNullOrEmpty (value = urlParams ["u"])) {
					logonDomain = (((pos = value.IndexOf ('\\')) > 0) ? value.Substring (0, pos) : string.Empty);
					logonUser = value.Substring (pos + 1);
					if ((logonDomain.Equals (Environment.UserDomainName, StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrEmpty (logonDomain)) && logonUser.Equals (Environment.UserName, StringComparison.InvariantCultureIgnoreCase))
						multiExplorerControl.logonDomain = multiExplorerControl.logonPass = multiExplorerControl.logonUser = string.Empty;
					else {
						multiExplorerControl.logonDomain = logonDomain;
						multiExplorerControl.logonUser = logonUser;
						multiExplorerControl.logonPass = string.Empty;
					}
				}
			} else {
				multiExplorerControl.hideNote = true;
				multiExplorerControl.allowListChange = multiExplorerControl.allowFolderChange = true;
			}
			multiExplorerControl.PropertyChanged += multiExplorerControl_PropertyChanged;
			//	raise multiExplorerControl_PropertyChanged once and init web browse
			Application.DoEvents ();
			multiExplorerControl.Url = multiExplorerControl.url;
		}

	}

}
