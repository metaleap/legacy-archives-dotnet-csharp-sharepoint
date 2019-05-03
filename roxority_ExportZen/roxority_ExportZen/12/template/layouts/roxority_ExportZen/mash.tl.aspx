<%@ Page Language="C#" AutoEventWireup="false" EnableEventValidation="false" EnableViewState="false" ValidateRequest="false" %>
<%@ Assembly Name="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Assembly Name="roxority_ExportZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
<%@ Import Namespace="roxority.Shared" %>
<%@ Import Namespace="roxority.SharePoint" %>
<%@ Import Namespace="roxority.Data" %>
<%@ Import Namespace="roxority_ExportZen" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%
	bool hasTv = (Array.IndexOf<string> (Request.QueryString.AllKeys, "tv") >= 0);
	string tmp;
	byte[]data;
	IDictionary fht, dyn;
	DataSource ds;
	if (Request.QueryString ["op"] == "gj") {
		Response.ContentType = "text/javascript";
		try {
			if (string.IsNullOrEmpty (tmp = ProductPage.GoogSrc))
				using (WebClient wc = new WebClient ())
					ProductPage.GoogSrc = tmp = (string.Empty + wc.DownloadString ((Request.IsSecureConnection ? "https" : "http") + "://www.google.com/jsapi")).Trim ().Trim ('\t', '\r', '\n');
			if (string.IsNullOrEmpty (tmp) || !tmp.Contains ("google.loader"))
				throw new Exception ();
			Response.Write (tmp);
		} catch {
			using (StreamReader sr = File.OpenText (Server.MapPath ("/_layouts/roxority_ExportZen/jsapi.tl.js")))
				Response.Write (sr.ReadToEnd ());
		}
	} else if (Request.QueryString ["op"] == "rf") {
		fht = new OrderedDictionary ();
		try {
			ds = DataSource.FromID (Request.QueryString ["dsid"], !"1".Equals (Request.QueryString ["ss"]), "1".Equals (Request.QueryString ["ss"]), Request.QueryString ["t"]);
			foreach (JsonSchemaManager.Property sp in ds.JsonSchema.Properties)
				if (!string.IsNullOrEmpty (tmp = Request.QueryString [sp.Name]))
					ds.JsonInstance [sp.Name] = (sp.PropertyType.IsBool ? "1".Equals (tmp) : (object) tmp);
			foreach (DictionaryEntry entry in ds.Properties.SortedByTitle)
				fht [entry.Key] = HttpUtility.HtmlEncode (entry.Value + string.Empty);
		} catch (Exception ex) {
			fht ["___roxerr"] = ex.ToString ();
		}
		Response.Write (JSON.JsonEncode (fht));
	} else if (Request.QueryString ["op"] == "imgoverlay")
		try {
			Response.ContentType = "image/png";
			using (Bitmap backImage = new Bitmap (Server.MapPath (Request.QueryString ["backimg"])))
			using (Bitmap overlayImage = new Bitmap (Server.MapPath (Request.QueryString ["overlay"])))
			using (Bitmap outImage = new Bitmap (backImage.Width, backImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)){
				using (Graphics gfx = Graphics.FromImage (outImage)) {
					if ((overlayImage.Width >= backImage.Width) || (overlayImage.Height >= backImage.Height))
						gfx.DrawImage (overlayImage, new Rectangle (0, 0, overlayImage.Width, overlayImage.Height));
					else {
						gfx.DrawImage (backImage, new Rectangle (0, 0, outImage.Width, outImage.Height));
						gfx.DrawImage (overlayImage, new Rectangle ((backImage.Width - overlayImage.Width) / 2, backImage.Height - overlayImage.Height, overlayImage.Width, overlayImage.Height));
					}
					gfx.Flush ();
				}
				outImage.Save (Response.OutputStream, ImageFormat.Png);
			}
		} catch {
			Response.Redirect ("/_layouts/images/blank.gif", true);
		} else if (Request.QueryString ["op"] == "vc") {
			Encoding enc = Encoding.Default;
			Response.ContentEncoding = enc;
			Response.ContentType = "text/x-vcard; charset=" + enc.WebName;
			Response.AddHeader ("Content-Disposition", "attachment; filename=\"" + Request.QueryString ["fn"] + "\"");
			Response.Write (UserDataSource.GetVcardExport (Request ["dsid"], int.Parse (Request.QueryString ["r"]), ProductPage.GetGuid (Request.QueryString ["i"], true)));
	} else if (Request.QueryString ["op"] == "o") {
		if (!string.IsNullOrEmpty (Request.QueryString ["ct"]))
			Response.ContentType = Request.QueryString ["ct"];
		if (!string.IsNullOrEmpty (Request.QueryString ["fn"]))
			Response.Headers.Add ("Content-Disposition", "attachment; filename=\"" + Request.QueryString ["fn"] + "\"");
		Response.Write (Request.QueryString ["o"]);
	} else if (Request.QueryString ["op"] == "r") {
	%>
jQuery(window).load(function() {
	setTimeout(function() { jQuery('.ms-bodyareacell').prepend('<%= SPEncode.ScriptEncode (Request.QueryString ["o"])%>'); }, 250);
	setTimeout(roxPostReady, 500);
});
	<%
} else if (Request.QueryString ["op"] == "cfe") {
	string webUrl = SPContext.Current.Web.Url.TrimEnd ('/');
	Guid listID = ProductPage.GetGuid (Request.QueryString ["lid"], true);
	SPSecurity.CatchAccessDeniedException = false;
	SPContext.Current.Site.CatchAccessDeniedException = false;
	ArrayList fileNames = new ArrayList ();
	if (!Guid.Empty.Equals (listID))
		try {
			ProductPage.Elevate (delegate () {
				SPList docLib;
				SPFile file;
				using (SPSite site = new SPSite (webUrl))
				using (SPWeb web = site.OpenWeb ())
					if ((docLib = web.Lists [listID]) != null)
						foreach (string fileName in Request.QueryString ["fn"].Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries)) {
							file = null;
							try {
								file = docLib.RootFolder.Files [Request.QueryString ["dp"].TrimEnd ('/') + "/" + fileName.TrimStart ('/')];
							} catch {
							}
							if (file != null)
								fileNames.Add (fileName);
						}

			}, true, true);
		} catch {
		}
	%><%= JSON.JsonEncode (fileNames)%><%
	} else if (Request.QueryString ["op"] != "noop") {
		fht = new Hashtable ();
		dyn = JSON.JsonDecode (Request ["dyn"]) as IDictionary;
		fht ["f"] = JSON.JsonDecode (Request ["f"]);
		fht ["fa"] = JSON.JsonDecode (Request ["fa"]);
		RollupWebPart.Render (null, __w, Request ["tid"], Request ["id"], int.Parse (Request ["ps"]), int.Parse (Request ["p"]), int.Parse (Request ["pmo"]), int.Parse (Request ["pst"]), int.Parse (Request ["psk"]), "1".Equals (Request ["dty"]), "1".Equals (Request ["did"]), "1".Equals (Request ["fl"]), Request ["pr"], "1".Equals (Request ["ls"]), "1".Equals (Request ["v"]), "1".Equals (Request ["s"]), Request ["spn"], "1".Equals (Request ["sd"]), Request ["tpn"], Request ["tpo"], hasTv ? Request ["tv"] : null, Request ["gpn"], "1".Equals (Request ["gd"]), "1".Equals (Request ["gb"]), "1".Equals (Request ["gs"]), "1".Equals (Request ["gi"]), "1".Equals (Request ["gid"]), int.Parse (Request ["rs"]), Request ["t"], int.Parse (Request ["nm"]), int.Parse (Request ["pm"]), "1".Equals (Request ["on"]), "1".Equals (Request ["vc"]), int.Parse (Request ["ih"]), Request ["la"], "1".Equals (Request ["ti"]), fht as Hashtable, null, Request ["dsid"], dyn);
	}
%>