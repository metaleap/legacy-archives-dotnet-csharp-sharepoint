﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LegacyMigration.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("LegacyMigration.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static System.Drawing.Bitmap arrow_rotate_clockwise {
            get {
                object obj = ResourceManager.GetObject("arrow_rotate_clockwise", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The following legacy Lists have been successfully deleted:.
        /// </summary>
        internal static string Deleted {
            get {
                return ResourceManager.GetString("Deleted", resourceCulture);
            }
        }
        
        internal static System.Drawing.Bitmap disk {
            get {
                object obj = ResourceManager.GetObject("disk", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error. Double-click to see all details.
        ///PREVIEW:.
        /// </summary>
        internal static string Error {
            get {
                return ResourceManager.GetString("Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The following legacy Lists could NOT be successfully deleted:.
        /// </summary>
        internal static string Failed {
            get {
                return ResourceManager.GetString("Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BEFORE you can upgrade ExportZen to 1.6 or higher, you NEED to successfully delete these legacy Lists. Try running this tool under a different account, or find another way of deleting these legacy Lists (with alternative methods, take care to not just recycle the legacy Lists, but fully delete them from this SharePoint system)..
        /// </summary>
        internal static string Failed2 {
            get {
                return ResourceManager.GetString("Failed2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #ID: export action definition.
        /// </summary>
        internal static string Item {
            get {
                return ResourceManager.GetString("Item", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (Legend).
        /// </summary>
        internal static string Legend {
            get {
                return ResourceManager.GetString("Legend", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Legacy List (with export action definitions).
        /// </summary>
        internal static string ListItems {
            get {
                return ResourceManager.GetString("ListItems", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Legacy List (no export action definitions).
        /// </summary>
        internal static string ListNoItems {
            get {
                return ResourceManager.GetString("ListNoItems", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migrated].
        /// </summary>
        internal static string Migrated {
            get {
                return ResourceManager.GetString("Migrated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No legacy Lists found in your farm. No backup is needed and no Lists need to be deleted. You can install ExportZen now..
        /// </summary>
        internal static string NoLists {
            get {
                return ResourceManager.GetString("NoLists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your backup file has been saved to:
        ///{0}
        ///You can import it (AFTER having fully upgraded ExportZen to 1.6 or higher) via Central Administration / Site Settings / ExportZen Studio / Transfer Wizard..
        /// </summary>
        internal static string Saved {
            get {
                return ResourceManager.GetString("Saved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Site Collection.
        /// </summary>
        internal static string Site {
            get {
                return ResourceManager.GetString("Site", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Step 1 (serialization) failed, process aborted:.
        /// </summary>
        internal static string Step1Failed {
            get {
                return ResourceManager.GetString("Step1Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Step 2 (storing backup file) failed, process aborted:
        ///
        ///{0}
        ///
        ///DO NOT EXIT this program before you have successfully saved the backup file somewhere else, anywhere. No legacy Lists have been deleted. Try again and pick another backup location..
        /// </summary>
        internal static string Step2Failed {
            get {
                return ResourceManager.GetString("Step2Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Web Application.
        /// </summary>
        internal static string WebApp {
            get {
                return ResourceManager.GetString("WebApp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Web Site (with legacy Lists and export action definitions).
        /// </summary>
        internal static string WebItems {
            get {
                return ResourceManager.GetString("WebItems", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Web Site (with legacy Lists and no export action definitions).
        /// </summary>
        internal static string WebNoItems {
            get {
                return ResourceManager.GetString("WebNoItems", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Web Site (no legacy Lists).
        /// </summary>
        internal static string WebNoLists {
            get {
                return ResourceManager.GetString("WebNoLists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service.
        /// </summary>
        internal static string WebSvc {
            get {
                return ResourceManager.GetString("WebSvc", resourceCulture);
            }
        }
    }
}
