using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebPartPages;
using WebPart = System.Web.UI.WebControls.WebParts.WebPart;

namespace DeployLabInventoryFilter {
	class Program {
		static void Main (string [] args) {
			string siteUrl = "http://roxbook/"; // "http://elndev.nzcorp.net/sites/labadmin/283";
			string pageUrl = "http://roxbook/FilterZen/NovoTest.aspx"; // "http://elndev.nzcorp.net/sites/labadmin/283/Lists/Enzymes/Available%20Enzymes.aspx";
			//string webPartName = "KWizCom_List_Filter_Plus_FromEnzymes.dwp";
			string webPartName = "FilterZen_Filter_Web_Part_Enzymes.webpart"; // "FilterZen_Filter_Web_Part_Enzymes.webpart";
			string zoneId = "Main";
			int zoneIndex = 0;

			using (SPSite spSite = new SPSite (siteUrl)) {
				using (SPWeb spWeb = spSite.OpenWeb ()) {

					AddWebPartToPage (spWeb, pageUrl, webPartName, zoneId, zoneIndex);
				}
			}
		}

		public static string AddWebPartToPage (
			  SPWeb web,
			  string pageUrl,
			  string webPartName,
			  string zoneID,
			  int zoneIndex) {
			using (WebPart webPart = CreateWebPart (web, webPartName)) {
				using (SPLimitedWebPartManager manager = web.GetLimitedWebPartManager (pageUrl, PersonalizationScope.Shared)) {
					manager.AddWebPart (webPart, zoneID, zoneIndex);
					//TODO: Will the list always be index = 0?
					foreach (WebPart listWebPart in manager.WebParts)
						if (listWebPart is ListViewWebPart) {
							//AddWebPartConnectionAspNet (web, pageUrl, webPart.ID, listWebPart.ID, "Send values as filters to", "Get Sort/Filter From");
							AddWebPartConnectionWss(web, pageUrl, webPart.ID, listWebPart.ID);
							break;
						}

					return webPart.ID;
				}
			}
		}

		private static WebPart CreateWebPart (SPWeb web, string webPartName) {
			SPQuery query = new SPQuery {
				Query = String.Format (
					"<Where><Eq><FieldRef Name='FileLeafRef'/><Value Type='File'>{0}</Value></Eq></Where>",
					webPartName)
			};

			SPList webPartGallery;

			if (null == web.ParentWeb) {
				// This is the root web.
				webPartGallery = web.GetCatalog (SPListTemplateType.WebPartCatalog);
			} else {
				// This is a sub-web.
				webPartGallery = web.ParentWeb.GetCatalog (SPListTemplateType.WebPartCatalog);
			}

			SPListItemCollection webParts = webPartGallery.GetItems (query);

			string typeName = webParts [0].GetFormattedValue ("WebPartTypeName");
			string assemblyName = webParts [0].GetFormattedValue ("WebPartAssembly");
			ObjectHandle webPartHandle = Activator.CreateInstance (assemblyName, typeName);

			if (webPartHandle != null) {
				WebPart webPart = (WebPart) webPartHandle.Unwrap ();
				return webPart;
			}

			return null;
		}

		public void SetWebPartProperty (
			SPWeb web,
			string pageUrl,
			string webPartID,
			string propertyName,
			string propertyValue) {
			using (SPLimitedWebPartManager manager = web.GetLimitedWebPartManager (pageUrl, PersonalizationScope.Shared)) {
				WebPart part = manager.WebParts [webPartID];
				Type runtimeType = part.GetType ();
				PropertyInfo property = runtimeType.GetProperty (propertyName);
				object value = ConvertValue (propertyValue, property.PropertyType);
				property.SetValue (part, value, null);
				manager.SaveChanges (part);
			}
		}

		public static void AddWebPartConnectionAspNet (
			SPWeb web,
			string pageUrl,
			string providerWebPartID,
			string consumerWebPartID,
			string providerConnectionPointName,
			string consumerConnectionPointName) {
			using (SPLimitedWebPartManager manager = web.GetLimitedWebPartManager (pageUrl, PersonalizationScope.Shared)) {
				WebPart provider = manager.WebParts [providerWebPartID];
				WebPart consumer = manager.WebParts [consumerWebPartID];

				ProviderConnectionPointCollection providerPoints = manager.GetProviderConnectionPoints (provider);
				ConsumerConnectionPointCollection consumerPoints = manager.GetConsumerConnectionPoints (consumer);

				ProviderConnectionPoint providerPoint = null;

				foreach (ProviderConnectionPoint point in providerPoints) {
					if (String.Equals (providerConnectionPointName, point.DisplayName, StringComparison.OrdinalIgnoreCase)) {
						providerPoint = point;
						break;
					}
				}

				ConsumerConnectionPoint consumerPoint = null;

				foreach (ConsumerConnectionPoint point in consumerPoints) {
					if (String.Equals (consumerConnectionPointName, point.DisplayName, StringComparison.OrdinalIgnoreCase)) {
						consumerPoint = point;
						break;
					}
				}

				manager.SPConnectWebParts (provider, providerPoint, consumer, consumerPoint);
			}
		}

		private static void AddWebPartConnectionWss (
			SPWeb web,
			string pageUrl,
			string providerWebPartID,
			string consumerWebPartID) {
			SPLimitedWebPartManager manager = web.GetLimitedWebPartManager (pageUrl, PersonalizationScope.Shared);

			Microsoft.SharePoint.WebPartPages.WebPart filterWp = (Microsoft.SharePoint.WebPartPages.WebPart) manager.WebParts [providerWebPartID];
			Microsoft.SharePoint.WebPartPages.WebPart listWp = (Microsoft.SharePoint.WebPartPages.WebPart) manager.WebParts [consumerWebPartID];

			if (filterWp.ConnectionID == Guid.Empty)
				filterWp.ConnectionID = Guid.NewGuid ();
			if (listWp.ConnectionID == Guid.Empty)
				listWp.ConnectionID = Guid.NewGuid ();

			//listWp.Connections = listWp.ConnectionID + "," +
			//     filterWp.ConnectionID + "," +
			//    "ListViewFilterConsumer_WPQ_" + "," +
			//    "" + "," +
			//    "ListViewFilterConsumer_WPQ_" + "," +
			//    "";
			listWp.Connections = listWp.ConnectionID + "," + filterWp.ConnectionID + ",ListViewFilterConsumer_WPQ_,roxorityFilterProviderInterface,ListViewFilterConsumer_WPQ_,roxorityFilterProviderInterface";
			manager.SaveChanges (filterWp);
			manager.SaveChanges (listWp);
		}

		private static object ConvertValue (string propertyValue, Type propertyType) {
			if (propertyValue != string.Empty) {
				object value = Convert.ChangeType (propertyValue, propertyType);
				return value;
			}
			return null;
		}
	}
}
