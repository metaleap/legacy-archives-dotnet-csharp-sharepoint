/******************************************************************/
/*                                                                */
/*                SharePoint Solution Installer                   */
/*                                                                */
/*    Copyright 2007 Lars Fastrup Nielsen. All rights reserved.   */
/*    http://www.fastrup.dk                                       */
/*                                                                */
/*    This program contains the confidential trade secret         */
/*    information of Lars Fastrup Nielsen.  Use, disclosure, or   */
/*    copying without written consent is strictly prohibited.     */
/*                                                                */
/* KML: Added SiteCollectionFeatureId                             */
/* KML: Updated InstallOperation enum to be public                */
/* KML: Added BackWardCompatibilityConfigProps                    */
/* KML: Added ConfigProps                                         */
/* KML: Added RequireMoss, FeatureScope, SolutionId, FeatureId,   */
/*      SiteCollectionRelativeConfigLink, SSPRelativeConfigLink,  */
/*      DefaultDeployToSRP, and DocumentationUrl properties       */
/* KML: Added ConfigProps                                         */
/*                                                                */
/******************************************************************/
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;

namespace roxority_SetupZen {

	using res = Properties.Resources;

	internal class InstallConfiguration {
		#region Constants

		public class BackwardCompatibilityConfigProps {
			// "Apllication" mispelled on purpose to match original mispelling released
			public const string RequireDeploymentToCentralAdminWebApllication = "RequireDeploymentToCentralAdminWebApllication";
			// Require="MOSS" = RequireMoss="true" 
			public const string Require = "Require";
			// FarmFeatureId = FeatureId with FeatureScope = Farm
			public const string FarmFeatureId = "FarmFeatureId";
		}

		public class ConfigProps {
			public const string BannerImage = "BannerImage";
			public const string LogoImage = "LogoImage";
			public const string EULA = "EULA";
			public const string RequireMoss = "RequireMoss";
			public const string UpgradeDescription = "UpgradeDescription";
			public const string RequireDeploymentToCentralAdminWebApplication = "RequireDeploymentToCentralAdminWebApplication";
			public const string RequireDeploymentToAllContentWebApplications = "RequireDeploymentToAllContentWebApplications";
			public const string SuggestDeploymentToCentralAdminWebApplication = "SuggestDeploymentToCentralAdminWebApplication";
			public const string SuggestDeploymentToAllContentWebApplications = "SuggestDeploymentToAllContentWebApplications";
			public const string DefaultDeployToSRP = "DefaultDeployToSRP";
			public const string Java = "Java";
			public const string Sql = "Sql";
			public const string SolutionId = "SolutionId";
			public const string SolutionFile = "SolutionFile";
			public const string LegacyLists = "LegacyLists";
			public const string UpdateMinVersion = "UpdateMinVersion";
			public const string MossAssemblyCheck12 = "MossAssemblyCheck12";
			public const string MossAssemblyCheck14 = "MossAssemblyCheck14";
			public const string SolutionFile14 = "SolutionFile14";
			public const string SolutionTitle = "SolutionTitle";
			public const string SolutionVersion = "SolutionVersion";
			public const string FeatureScope = "FeatureScope";
			public const string FeatureId = "FeatureId";
			public const string SiteCollectionRelativeConfigLink = "SiteCollectionRelativeConfigLink";
			public const string SSPRelativeConfigLink = "SSPRelativeConfigLink";
			public const string DocumentationUrl = "DocumentationUrl";
			public const string Vendor = "Vendor";
		}

		#endregion

		#region Internal Static Properties

		internal static string BannerImage {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.BannerImage];
			}
		}

		internal static string LogoImage {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.LogoImage];
			}
		}

		internal static string LegacyLists {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.LegacyLists];
			}
		}

		internal static string UpdateMinVersion {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.UpdateMinVersion];
			}
		}

		internal static string MossAssemblyCheck12 {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.MossAssemblyCheck12];
			}
		}

		internal static string MossAssemblyCheck14 {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.MossAssemblyCheck14];
			}
		}

		internal static bool RequireMoss {
			get {
				bool rtnValue = false;
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.RequireMoss];
				if (String.IsNullOrEmpty (valueStr)) {
					valueStr = ConfigurationManager.AppSettings [BackwardCompatibilityConfigProps.Require];
					rtnValue = valueStr != null && valueStr.Equals ("MOSS", StringComparison.OrdinalIgnoreCase);
				} else {
					rtnValue = Boolean.Parse (valueStr);
				}
				return rtnValue;
			}
		}

		internal static Guid SolutionId {
			get {
				return new Guid (ConfigurationManager.AppSettings [ConfigProps.SolutionId]);
			}
		}

		internal static string SolutionFile {
			get {
				bool is14 = ProductPage.Is14;
				string tmp, v = ConfigurationManager.AppSettings [is14 ? ConfigProps.SolutionFile14 : ConfigProps.SolutionFile];
				if (File.Exists (tmp = (v.Substring (0, v.LastIndexOf ('.')) + "_wss.wsp")) && !SystemCheckControl.SystemCheck.IsMOSSInstalled)
					v = tmp;
				return ((is14 && string.IsNullOrEmpty (v)) ? ConfigurationManager.AppSettings [ConfigProps.SolutionFile] : v);
			}
		}

		internal static string SolutionName {
			get {
				string name = SolutionFile;
				int pos = name.LastIndexOf ('.');
				if (pos > 0)
					name = name.Substring (0, pos);
				if ((pos = name.LastIndexOf ('\\')) > 0)
					name = name.Substring (pos + 1);
				if (name.EndsWith ("14"))
					name = name.Substring (0, name.Length - 2);
				return name;
			}
		}

		internal static string SolutionNameCore {
			get {
				return SolutionName.Replace ("_xiv", string.Empty).Replace ("_xii", string.Empty).Replace ("_wss", string.Empty);
			}
		}

		internal static string SolutionShortName {
			get {
				string name = SolutionNameCore;
				int pos = name.IndexOf ('_'), pos2 = name.LastIndexOf ('_');
				return ((pos > 0) ? ((pos2 == pos) ? name.Substring (pos + 1) : name.Substring (pos + 1, pos2 - (pos + 1))) : name);
			}
		}

		internal static string SolutionTitle {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.SolutionTitle];
			}
		}

		internal static Version SolutionVersion {
			get {
				return new Version (ConfigurationManager.AppSettings [ConfigProps.SolutionVersion]);
			}
		}

		internal static string UpgradeDescription {
			get {
				string str = ConfigurationManager.AppSettings [ConfigProps.UpgradeDescription];
				if (!string.IsNullOrEmpty(str)) {
					str = FormatString (str);
				}
				return str;
			}
		}

		internal static SPFeatureScope FeatureScope {
			get {
				// Default to farm features as this is what the installer only supported initially
				SPFeatureScope featureScope = SPFeatureScope.Farm;
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.FeatureScope];
				if (!String.IsNullOrEmpty (valueStr)) {
					featureScope = (SPFeatureScope) Enum.Parse (typeof (SPFeatureScope), valueStr, true);
				}
				return featureScope;
			}
		}

		// Modif JPI - Début
		internal static List<Guid?> FeatureId {
			get {
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.FeatureId];

				//
				// Backwards compatibility with old configuration files before site collection features allowed
				//
				if (String.IsNullOrEmpty (valueStr)) {
					valueStr = ConfigurationManager.AppSettings [BackwardCompatibilityConfigProps.FarmFeatureId];
				}

				if (!String.IsNullOrEmpty (valueStr)) {
					string [] _strGuidArray = valueStr.Split (";".ToCharArray ());
					if (_strGuidArray.Length >= 0) {
						List<Guid?> _guidArray = new List<Guid?> ();
						foreach (string _strGuid in _strGuidArray) {
							_guidArray.Add (new Guid (_strGuid));
						}
						return _guidArray;
					}
				}

				return null;
			}
		}
		// Modif JPI - Fin

		internal static bool RequireDeploymentToCentralAdminWebApplication {
			get {
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.RequireDeploymentToCentralAdminWebApplication];

				//
				// Backwards compatability with old configuration files containing spelling error in the 
				// application setting key (Bug 990).
				//
				if (String.IsNullOrEmpty (valueStr)) {
					valueStr = ConfigurationManager.AppSettings [BackwardCompatibilityConfigProps.RequireDeploymentToCentralAdminWebApllication];
				}

				if (!String.IsNullOrEmpty (valueStr)) {
					return valueStr.Equals ("true", StringComparison.OrdinalIgnoreCase);
				}

				return false;
			}
		}

		internal static bool SuggestDeploymentToCentralAdminWebApplication {
			get {
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.SuggestDeploymentToCentralAdminWebApplication];
				if (!String.IsNullOrEmpty (valueStr)) {
					return valueStr.Equals ("true", StringComparison.OrdinalIgnoreCase);
				}

				return false;
			}
		}

		internal static bool RequireDeploymentToAllContentWebApplications {
			get {
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.RequireDeploymentToAllContentWebApplications];
				if (!String.IsNullOrEmpty (valueStr)) {
					return valueStr.Equals ("true", StringComparison.OrdinalIgnoreCase);
				}
				return false;
			}
		}

		internal static bool SuggestDeploymentToAllContentWebApplications {
			get {
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.SuggestDeploymentToAllContentWebApplications];
				if (!String.IsNullOrEmpty (valueStr)) {
					return valueStr.Equals ("true", StringComparison.OrdinalIgnoreCase);
				}
				return false;
			}
		}

		internal static string Vendor {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.Vendor];
			}
		}

		internal static string Java {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.Java];
			}
		}

		internal static string Sql {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.Sql];
			}
		}

		internal static bool DefaultDeployToSRP {
			get {
				string valueStr = ConfigurationManager.AppSettings [ConfigProps.DefaultDeployToSRP];
				if (!String.IsNullOrEmpty (valueStr))
					return valueStr.Equals ("true", StringComparison.OrdinalIgnoreCase);
				return false;
			}
		}

		internal static Version InstalledVersion {
			get {
				Version v1 = null, v2 = null;
				try {
					v1 = new Version (SPFarm.Local.Properties ["Solution_" + SolutionId.ToString () + "_Version"] + string.Empty);
				} catch (NullReferenceException ex) {
					throw new InstallException (res.InstallException, ex);
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				} catch {
				}
				try {
					v2 = new Version (((AssemblyFileVersionAttribute) (Assembly.Load (InstallConfiguration.SolutionNameCore + ", Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01").GetCustomAttributes (typeof (AssemblyFileVersionAttribute), true) [0])).Version);
				} catch {
				}
				return ((v2 == null) ? v1 : ((v1 == null) ? v2 : ((v2 > v1) ? v2 : v1)));
			}

			set {
				try {
					SPFarm farm = SPFarm.Local;
					string key = "Solution_" + SolutionId.ToString () + "_Version";
					farm.Properties [key] = value;
					farm.Update ();
				} catch (NullReferenceException ex) {
					throw new InstallException (res.InstallException, ex);
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}
		}

		public static bool ShowFinishedControl {
			get {
				return !String.IsNullOrEmpty (ConfigurationManager.AppSettings [ConfigProps.SiteCollectionRelativeConfigLink]) ||
				  !String.IsNullOrEmpty (ConfigurationManager.AppSettings [ConfigProps.SSPRelativeConfigLink]);
			}
		}

		public static string GetConfigLink (string tab) {
			foreach (SPWebApplication webApp in ProductPage.TryEach<SPWebApplication> (SPWebService.ContentService.WebApplications))
				if (!webApp.Properties.ContainsKey ("Microsoft.Office.Server.SharedResourceProvider"))
					foreach (SPSite site in webApp.Sites)
						using (site)
							return ProductPage.MergeUrlPaths (site.Url, "_layouts/" + SolutionNameCore + ".aspx?cfg=" + tab);
			return "http://localhost/_layouts/" + SolutionNameCore + ".aspx?cfg=" + tab;
		}

		public static string SiteCollectionRelativeConfigLink {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.SiteCollectionRelativeConfigLink];
			}
		}

		public static string SSPRelativeConfigLink {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.SSPRelativeConfigLink];
			}
		}

		public static string DocumentationUrl {
			get {
				return ConfigurationManager.AppSettings [ConfigProps.DocumentationUrl];
			}
		}

		#endregion

		#region Internal Static Methods

		internal static string FormatString (string str) {
			return FormatString (str, null);
		}

		internal static string FormatString (string str, params object [] args) {
			str = str.Replace ("{SolutionTitle}", InstallConfiguration.SolutionTitle);
			if (args != null)
				str = String.Format (str, args);
			return str;
		}

		internal static string GetEula (string prefix) {
			return ConfigurationManager.AppSettings [prefix + ConfigProps.EULA];
		}

		#endregion
	}

	public enum InstallOperation {
		Install,
		Upgrade,
		Repair,
		Uninstall
	}

}
