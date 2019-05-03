
using Microsoft.Office.Server;
using Microsoft.Office.Server.UserProfiles;
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Create1000Users {

	public class Program {

		internal const int DB_BEGIN_AT = 90;
		internal const int USER_BEGIN_AT = 90;
		internal const int NUM_USERS = 600;

		internal static string CreateLocalUser (string username, string password, string homedir) {
			if (!Directory.Exists (homedir))
				Directory.CreateDirectory (homedir);
			using (Process proc = new Process ()) {
				proc.StartInfo.WorkingDirectory = @"C:\Windows\System32";
				proc.StartInfo.FileName = "net.exe";
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardError = true;
				proc.StartInfo.RedirectStandardInput = true;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				proc.StartInfo.Arguments = " user " + username + " " + password + " /ADD /ACTIVE:YES " + @"/EXPIRES:NEVER /FULLNAME:" + username + @" /HOMEDIR:""" + homedir + @""" /PASSWORDCHG:NO /PASSWORDREQ:YES";
				proc.Start ();
				proc.WaitForExit ();
				proc.Close ();
			}
			return username;
		}

		internal static string PickSome (string [] allValues, Random rnd, UserProfileValueCollection userVals, int count) {
			string temp = string.Empty;
			List<string> vals = new List<string> ();
			for (int c = 0; c < count; c++) {
				while (string.IsNullOrEmpty (temp) || vals.Contains (temp))
					temp = allValues [rnd.Next (0, allValues.Length)];
				if (userVals != null)
					userVals.Add (temp);
				vals.Add (temp);
			}
			return string.Join (", ", vals.ToArray ());
		}

		private static void fixupRes () {
			XmlDocument docDst = new XmlDocument ();
			XmlDocument docSrc = new XmlDocument ();
			XmlNode node;
			string srcPath = @"C:\Users\roxor\Documents\Visual Studio 2010\Projects\roxority_Shared\Properties\roxority_Shared.de.resx";
			string dstPath = @"C:\Users\roxor\Documents\Visual Studio 2010\Projects\roxority_PeopleZen\roxority_PeopleZen\Properties\RollupZen.de.resx";
			docSrc.Load (srcPath);
			docDst.Load (dstPath);
			foreach (XmlNode n in docDst.DocumentElement.SelectNodes ("//data")) {
				if ((node = docSrc.DocumentElement.SelectSingleNode ("//data[@name='" + n.Attributes ["name"].Value + "']")) != null) {
					n.SelectSingleNode ("value").InnerText = node.SelectSingleNode ("value").InnerText;
					node.ParentNode.RemoveChild (node);
				}
			}
			docSrc.Save (srcPath);
			docDst.Save (dstPath);
		}

		public static void Main (string [] args) {
			return;
			string [] firstNames = { "Tim", "Nick", "Dagny", "Steve", "Bill", "Ayn", "Marilyn", "Joanna", "John", "Matt", "Chris", "Phil", "Tom", "Paul", "Walt", "Michael" },
				lastNames = { "Jobs", "Gates", "Werner", "Schumann", "Truxa", "Rand", "Monroe", "Wojtek", "Ferris", "Hornby", "Taggart", "Walker", "Maroon", "Peters", "Graham", "Disney", "Meier" },
				depts = { "Engineering", "Quality Assurance", "PR / Marketing", "Human Resources", "Finance", "IT", "Software Development" },
				skills = { "C++", "JavaScript", "Python", "Lisp", "C#", "F#", "Scheme", "OCaml", "Caml", "ObjectiveC", "SmallTalk", "Haskell", "Ruby" },
				resps = { "Project Management", "Product Management", "QA", "Documentation", "Tech Support", "Programming", "Systems Analysis", "Design", "Information Architecture", "Controlling" };
			string firstName, lastName;
			Random rnd = new Random ();
			using (IDbConnection conn = new SqlConnection ("Data Source=ROXWIN7\\SHAREPOINT; Initial Catalog=roxority; Integrated Security=SSPI")) {
				conn.Open ();
				using (IDbTransaction trans = conn.BeginTransaction ())
				using (IDbCommand cmd = new SqlCommand ("", conn as SqlConnection, trans as SqlTransaction)) {
					//Microsoft.SharePoint.SPSecurity.RunWithElevatedPrivileges (delegate () {
					UserProfileManager man = new UserProfileManager (ServerContext.GetContext (ProductPage.GetAdminSite ()), false, true);
					UserProfile user;
					try {
						for (int i = 0; i < NUM_USERS; i++) {
							firstName = firstNames [rnd.Next (0, firstNames.Length)];
							lastName = lastNames [rnd.Next (0, lastNames.Length)];
							if (i >= USER_BEGIN_AT) {
								Console.Title = CreateLocalUser ("roxor_" + i, "roxor123", @"c:\users\roxor_n");
								user = man.CreateUserProfile (Environment.MachineName.ToUpperInvariant () + "\\roxor_" + i, firstName + " " + lastName);
								user ["FirstName"].Value = firstName;
								user ["LastName"].Value = lastName;
								user ["WorkPhone"].Value = i.ToString ();
								user ["Department"].Value = depts [rnd.Next (0, depts.Length)];
								user ["AboutMe"].Value = "Batch-generated #" + i;
								PickSome (skills, rnd, user ["SPS-Skills"], rnd.Next (2, 6));
								PickSome (resps, rnd, user ["SPS-Responsibility"], rnd.Next (2, 6));
								user ["WorkEmail"].Value = firstName.ToLowerInvariant () + "." + lastName.ToLowerInvariant () + "@roxority.com";
								user.Commit ();
								Console.Title = user.PublicUrl + string.Empty;
							}
							if (i >= DB_BEGIN_AT) {
								cmd.CommandText = string.Format ("INSERT INTO dbo.roxUsers (FirstName, LastName, FullName, UserName, Email, Department, Skills, AboutMe, Responsibility, Phone) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}')", firstName, lastName, firstName + " " + lastName, "roxor_" + i, firstName.ToLowerInvariant () + "." + lastName.ToLowerInvariant () + "@roxority.com", depts [rnd.Next (0, depts.Length)], PickSome (skills, rnd, null, rnd.Next (2, 6)), "Batch-generated #" + i, PickSome (resps, rnd, null, rnd.Next (2, 6)), i);
								cmd.ExecuteNonQuery ();
							}
						}
						trans.Commit ();
					} catch {
						trans.Rollback ();
						throw;
					}
					//});
				}
			}
			Console.WriteLine ("done.");
		}

	}

}
