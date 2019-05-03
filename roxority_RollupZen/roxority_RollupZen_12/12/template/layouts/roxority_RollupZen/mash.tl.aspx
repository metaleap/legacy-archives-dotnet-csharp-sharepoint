<%@ Page Language="C#" AutoEventWireup="false" EnableEventValidation="false" EnableViewState="false" ValidateRequest="false" %>
<%@ Assembly Name="roxority_RollupZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
<%@ Import Namespace="roxority.Shared" %>
<%@ Import Namespace="roxority.SharePoint" %>
<%@ Import Namespace="roxority.Data" %>
<%@ Import Namespace="roxority_RollupZen" %>
<%
	bool hasTv = (Array.IndexOf<string> (Request.QueryString.AllKeys, "tv") >= 0);
	string tmp;
	IDictionary fht, dyn;
	DataSource ds;
	if (Request.QueryString ["op"] == "rf") {
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
	} else {
		fht = new Hashtable ();
		dyn = JSON.JsonDecode (Request ["dyn"]) as IDictionary;
		fht ["f"] = JSON.JsonDecode (Request ["f"]);
		fht ["fa"] = JSON.JsonDecode (Request ["fa"]);
		RollupWebPart.Render (null, __w, Request ["tid"], Request ["id"], int.Parse (Request ["ps"]), int.Parse (Request ["p"]), int.Parse (Request ["pmo"]), int.Parse (Request ["pst"]), int.Parse (Request ["psk"]), "1".Equals (Request ["dty"]), "1".Equals (Request ["did"]), "1".Equals (Request ["fl"]), Request ["pr"], "1".Equals (Request ["ls"]), "1".Equals (Request ["v"]), "1".Equals (Request ["s"]), Request ["spn"], "1".Equals (Request ["sd"]), Request ["tpn"], Request ["tpo"], hasTv ? Request ["tv"] : null, Request ["gpn"], "1".Equals (Request ["gd"]), "1".Equals (Request ["gb"]), "1".Equals (Request ["gs"]), "1".Equals (Request ["gi"]), "1".Equals (Request ["gid"]), int.Parse (Request ["rs"]), Request ["t"], int.Parse (Request ["nm"]), int.Parse (Request ["pm"]), "1".Equals (Request ["on"]), int.Parse (Request ["ih"]), Request ["la"], "1".Equals (Request ["ti"]), fht as Hashtable, null, Request ["dsid"], dyn);
	}
%>