using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;

namespace NovoPart {
	[Guid ("d6567232-9692-4887-a39a-38703bd2d0b2")]
	public class NovoPart : Microsoft.SharePoint.WebPartPages.WebPart {
		private bool _error = false;
		private string _myProperty = null;


		[Personalizable (PersonalizationScope.Shared)]
		[WebBrowsable (true)]
		[System.ComponentModel.Category ("My Property Group")]
		[WebDisplayName ("MyProperty")]
		[WebDescription ("Meaningless Property")]
		public string MyProperty {
			get {
				if (_myProperty == null) {
					_myProperty = "Hello SharePoint";
				}
				return _myProperty;
			}
			set {
				_myProperty = value;
			}
		}


		public NovoPart () {
			this.ExportMode = WebPartExportMode.All;
		}

		/// <summary>
		/// Create all your controls here for rendering.
		/// Try to avoid using the RenderWebPart() method.
		/// </summary>
		protected override void CreateChildControls () {
			if (!_error) {
				try {

					base.CreateChildControls ();

					// Your code here...
					this.Controls.Add (new LiteralControl (this.MyProperty));
				} catch (Exception ex) {
					HandleException (ex);
				}
			}
		}

		/// <summary>
		/// Ensures that the CreateChildControls() is called before events.
		/// Use CreateChildControls() to create your controls.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad (EventArgs e) {
			if (!_error) {
				try {
					base.OnLoad (e);
					this.EnsureChildControls ();

					// Your code here...
				} catch (Exception ex) {
					HandleException (ex);
				}
			}
		}

		/// <summary>
		/// Clear all child controls and add an error message for display.
		/// </summary>
		/// <param name="ex"></param>
		private void HandleException (Exception ex) {
			this._error = true;
			this.Controls.Clear ();
			this.Controls.Add (new LiteralControl (ex.Message));
		}
	}
}
