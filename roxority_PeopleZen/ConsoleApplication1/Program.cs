
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1 {

	public class Program {

		public static void TestDateTimeFormats () {
			string x;
			while (!string.IsNullOrEmpty (x = Console.ReadLine ()))
				try {
					Console.WriteLine (DateTime.Now.ToString (x));
				} catch (Exception ex) {
					Console.WriteLine (ex.Message);
				}
		}

		public static void Main (string [] args) {
			TestDateTimeFormats ();
		}

	}

}
