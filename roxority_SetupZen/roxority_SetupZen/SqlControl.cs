
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace roxority_SetupZen {

	using res = Properties.Resources;

	public partial class SqlControl : InstallerControl {

		public static string SuccessDatabase = Path.GetFileNameWithoutExtension (InstallConfiguration.Sql);
		public static string SuccessInstance = string.Empty;

		public SqlControl () {
			InitializeComponent ();
		}

		private void sqlButton_Click (object sender, EventArgs e) {
			bool exists = true, useThis = false;
			string tmp;
			string [] expand;
			int len;
			List<string> ddl = new List<string> ();
			Form.NextButton.Enabled = Enabled = false;
			try {
				using (IDbConnection conn = new SqlConnection ("Data Source=" + sqlDataSourceComboBox.Text + "; Initial Catalog=master; Integrated Security=SSPI"))
				using (IDbCommand cmd = conn.CreateCommand ()) {
					conn.Open ();
					try {
						conn.ChangeDatabase (sqlDatabaseTextBox.Text);
						cmd.CommandText = "SELECT * FROM " + ConfigurationManager.AppSettings ["SqlTable"];
						using (IDataReader dr = cmd.ExecuteReader ())
							dr.Read ();
					} catch (SqlException) {
						exists = false;
					}
					if (exists)
						useThis = (MessageBox.Show (this, res.SqlUseThis, sqlDatabaseTextBox.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes);
					else {
						using (StreamReader sr = File.OpenText ((InstallConfiguration.Sql.Contains ("\\")) ? InstallConfiguration.Sql : Path.Combine (Application.StartupPath, InstallConfiguration.Sql.TrimStart ('\\'))))
							while (!string.IsNullOrEmpty (tmp = sr.ReadLine ()))
								ddl.Add (tmp);
						if ((!string.IsNullOrEmpty (ConfigurationManager.AppSettings ["SqlExpand"])) && ((expand = ConfigurationManager.AppSettings ["SqlExpand"].Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (expand.Length > 1)) {
							len = ddl.Count;
							for (int j = 1; j < expand.Length; j++)
								for (int i = 0; i < len; i++)
									ddl.Add (ddl [i].Replace (expand [0], expand [j]));
							for (int i = 0; i < len; i++)
								ddl.RemoveAt (0);
						}
						cmd.CommandText = "CREATE DATABASE " + sqlDatabaseTextBox.Text;
						cmd.ExecuteNonQuery ();
						conn.ChangeDatabase (sqlDatabaseTextBox.Text);
						using (IDbTransaction transaction = cmd.Transaction = conn.BeginTransaction ())
							try {
								foreach (string ddlCmd in ddl) {
									cmd.CommandText = ddlCmd;
									cmd.ExecuteNonQuery ();
								}
								transaction.Commit ();
							} catch {
								transaction.Rollback ();
								throw;
							}
						MessageBox.Show (this, res.SqlCreated, sqlDatabaseTextBox.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
						useThis = true;
					}
				}
			} catch (Exception ex) {
				MessageBox.Show (this, ex.Message, ex.GetType ().Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			Enabled = true;
			if (Form.NextButton.Enabled = useThis)
				try {
					ProductPage.Config<string> (null, "SqlDatabase", SuccessDatabase = sqlDatabaseTextBox.Text, false);
					ProductPage.Config<string> (null, "SqlInstance", SuccessInstance = sqlDataSourceComboBox.Text, false);
				} catch (Exception ex) {
					Form.NextButton.Enabled = false;
					MessageBox.Show (this, ex.Message, ex.GetType ().Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
		}

		private void sqlDatabaseTextBox_TextChanged (object sender, EventArgs e) {
			sqlButton.Enabled = ((!string.IsNullOrEmpty (sqlDataSourceComboBox.Text.Trim ())) && (!string.IsNullOrEmpty (sqlDatabaseTextBox.Text.Trim ())));
		}

		internal void detectInstances () {
			Exception error = null;
			if (Debugger.IsAttached && sqlDataSourceComboBox.InvokeRequired) {
				sqlDataSourceComboBox.Invoke (new ThreadStart (detectInstances));
				return;
			}
			sqlDataSourceComboBox.Items.Clear ();
			try {
				using (DataTable dt = SqlDataSourceEnumerator.Instance.GetDataSources ())
					foreach (DataRow row in dt.Rows)
						sqlDataSourceComboBox.Items.Add (row ["ServerName"] + "\\" + row ["InstanceName"]);
			} catch (Exception ex) {
				error = ex;
			}
			sqlDatabaseTextBox.Enabled = sqlDataSourceComboBox.Enabled = true;
			if (error != null)
				sqlDataSourceStatusLabel.Text = error.Message;
			else if (sqlDataSourceComboBox.Items.Count == 0)
				sqlDataSourceStatusLabel.Text = res.SqlNoInstances;
			else
				sqlDataSourceStatusLabel.Text = string.Format (res.SqlInstances, sqlDataSourceComboBox.Items.Count);
			sqlDatabaseTextBox_TextChanged (sqlDatabaseTextBox, EventArgs.Empty);
			sqlDataSourceComboBox.Focus ();
		}

		protected internal override void Open (InstallOptions options) {
			Form.NextButton.Enabled = sqlButton.Enabled = sqlDataSourceComboBox.Enabled = sqlDatabaseTextBox.Enabled = false;
			sqlDatabaseStatusLabel.Text = string.Empty;
			sqlDataSourceComboBox.Text = SuccessInstance;
			sqlDatabaseTextBox.Text = SuccessDatabase;
			sqlDataSourceComboBox.Items.Clear ();
			sqlDataSourceStatusLabel.Text = res.SqlInstanceWait;
			Application.DoEvents ();
			new Thread (detectInstances).Start ();
		}

	}

}
