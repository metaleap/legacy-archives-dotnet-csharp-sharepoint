
using System;

namespace roxority.SharePoint {

	public class Program {

		public static void Main (string [] args) {
			string fileName = ProductPage.AssemblyName + ".export.rox";
			if ((args != null) && (args.Length > 0) && !string.IsNullOrEmpty (args [0]))
				fileName = args [0];
			Console.Write ("Importing {0}... ", fileName);
			ProductPage.ImportFarmSettings (fileName);
			Console.WriteLine ("DONE.");
		}

	}

}
