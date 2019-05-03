using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Security;

namespace roxority_LookupZen
{
    // TODO: Replace, as needed, "SPFieldText" with some other class derived from SPField. 
    // TODO: Update, as needed, ParentType element in fldtypes*.xml in this solution. 
    [CLSCompliant(false)]
    [Guid("8c5150a7-4d6f-4830-bc39-1efce6c67ca5")]
    public class roxority_LookupZenField : SPFieldText
    {
        public roxority_LookupZenField(SPFieldCollection fields, string fieldName)
            : base(fields, fieldName)
        {
        }
        
        public roxority_LookupZenField(SPFieldCollection fields, string typeName, string displayName)
            : base(fields, typeName, displayName)
        {
        }

        public override BaseFieldControl FieldRenderingControl
        {
            [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
            get
            {
                BaseFieldControl fieldControl = new roxority_LookupZenFieldControl();
                fieldControl.FieldName = this.InternalName;

                return fieldControl;
            }
        }
    }
}
