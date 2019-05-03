/**************************************************************************************/
/*                                                                                    */
/*                         SharePoint Solution Installer                              */
/*                 http://www.codeplex.com/sharepointinstaller                        */
/*                                                                                    */
/*            (c) Copyright 2007-2008 Lars Fastrup Nielsen.                           */
/*                                                                                    */
/*  This source is subject to the Microsoft Permissive License.                       */
/*  http://www.codeplex.com/sharepointinstaller/Project/License.aspx                  */
/*                                                                                    */
/* KML: Updated Open() to handle Site Collection Features                             */
/* KML: Added ActivateSiteCollectionFeatureCommand                                    */
/* KML: Added DeactivateSIteCollectionFeatureCommand                                  */
/* KML: Moved InstallOperation to be owned by the InstallerForm                       */
/* KML: Updated to new FeatureScope configuration property                            */
/* LFN 2008-06-15: Fixed bugs in the DeactivateSiteCollectionFeatureCommand class     */
/*                                                                                    */
/**************************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using roxority.SharePoint;
using Microsoft.SharePoint.Administration;

using Microsoft.SharePoint;
using System.IO;


namespace roxority_SetupZen {

	using res = Properties.Resources;

	public partial class InstallProcessControl : InstallerControl {
		private static readonly MessageCollector log = new MessageCollector (LogManager.GetLogger ());

		private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes (15);

		private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer ();
		private CommandList executeCommands;
		private CommandList rollbackCommands;
		private int nextCommand;
		private bool completed;
		private bool requestCancel;
		private int errors;
		private int rollbackErrors;

		public InstallProcessControl () {
			InitializeComponent ();

			errorPictureBox.Visible = false;
			errorDetailsTextBox.Visible = false;

			this.Load += new EventHandler (InstallProcessControl_Load);
		}

		#region Event Handlers

		private void InstallProcessControl_Load (object sender, EventArgs e) {
			switch (Form.Operation) {
				case InstallOperation.Install:
					Form.SetTitle (res.Installing);
					Form.SetSubTitle (InstallConfiguration.FormatString (res.WaitInstalling));
					break;

				case InstallOperation.Upgrade:
					Form.SetTitle (res.Upgrading);
					Form.SetSubTitle (InstallConfiguration.FormatString (res.WaitUpgrading));
					break;

				case InstallOperation.Repair:
					Form.SetTitle (res.Repairing);
					Form.SetSubTitle (InstallConfiguration.FormatString (res.WaitRepairing));
					break;

				case InstallOperation.Uninstall:
					Form.SetTitle (res.Removing);
					Form.SetSubTitle (InstallConfiguration.FormatString (res.WaitRemoving));
					break;
			}

			Form.PrevButton.Enabled = false;
			Form.NextButton.Enabled = false;
		}

		private void TimerEventInstall (Object myObject, EventArgs myEventArgs) {
			timer.Stop ();

			if (requestCancel) {
				descriptionLabel.Text = res.CancelRollback;
				InitiateRollback ();
			} else if (nextCommand < executeCommands.Count) {
				try {
					Command command = executeCommands [nextCommand];
					if (command.Execute ()) {
						nextCommand++;
						progressBar.PerformStep ();

						if (nextCommand < executeCommands.Count) {
							descriptionLabel.Text = executeCommands [nextCommand].Description;
						}
					}
					timer.Start ();
				} catch (Exception ex) {
					log.Error (res.Error);
					log.Error (ex.Message, ex);

					errors++;
					errorPictureBox.Visible = true;
					errorDetailsTextBox.Visible = true;
					errorDetailsTextBox.Text = ex.Message;

					descriptionLabel.Text = res.FinFailAll;
					InitiateRollback ();
				}
			} else {
				descriptionLabel.Text = res.FinSuccAll;
				HandleCompletion ();
			}
		}

		private void TimerEventRollback (Object myObject, EventArgs myEventArgs) {
			timer.Stop ();

			if (nextCommand < rollbackCommands.Count) {
				try {
					Command command = rollbackCommands [nextCommand];
					if (command.Rollback ()) {
						nextCommand++;
						progressBar.PerformStep ();
					}
				} catch (Exception ex) {
					log.Error (res.Error);
					log.Error (ex.Message, ex);

					rollbackErrors++;
					nextCommand++;
					progressBar.PerformStep ();
				}

				timer.Start ();
			} else {
				if (rollbackErrors == 0) {
					descriptionLabel.Text = res.FinSuccRollback;
				} else {
					descriptionLabel.Text = string.Format (res.FinFailRollback, rollbackErrors);
				}

				HandleCompletion ();
			}
		}

		#endregion

		#region Protected Methods

		protected internal override void RequestCancel () {
			if (completed) {
				base.RequestCancel ();
			} else {
				requestCancel = true;
				Form.AbortButton.Enabled = false;
			}
		}

		protected internal override void Open (InstallOptions options) {
			executeCommands = new CommandList ();
			rollbackCommands = new CommandList ();
			nextCommand = 0;
			SPFeatureScope featureScope = InstallConfiguration.FeatureScope;
			DeactivateSiteCollectionFeatureCommand deactivateSiteCollectionFeatureCommand = null;

			switch (Form.Operation) {
				case InstallOperation.Install:
					executeCommands.Add (new AddSolutionCommand (this));
					if ((options.WebApplicationTargets != null) && (options.WebApplicationTargets.Count > 0)) {
						executeCommands.Add (new CreateDeploymentJobCommand (this, options.WebApplicationTargets));
					} else {
						// KML TODO - need to get rid of options.Targets? - check with Lars
						executeCommands.Add (new CreateDeploymentJobCommand (this, options.Targets));
					}
					executeCommands.Add (new WaitForJobCompletionCommand (this, res.WaitDeploy));
					if (featureScope == SPFeatureScope.Farm) {
						executeCommands.Add (new ActivateFarmFeatureCommand (this));
					} else if (featureScope == SPFeatureScope.Site) {
						executeCommands.Add (new ActivateSiteCollectionFeatureCommand (this, options.SiteCollectionTargets));
					}
					executeCommands.Add (new RegisterVersionNumberCommand (this));

					for (int i = executeCommands.Count - 1; i <= 0; i--) {
						rollbackCommands.Add (executeCommands [i]);
					}
					break;

				case InstallOperation.Upgrade:
					if (featureScope == SPFeatureScope.Farm) {
						executeCommands.Add (new DeactivateFarmFeatureCommand (this));
					} else if (featureScope == SPFeatureScope.Site) {
						deactivateSiteCollectionFeatureCommand = new DeactivateSiteCollectionFeatureCommand (this);
						executeCommands.Add (deactivateSiteCollectionFeatureCommand);
					}
					if (!IsSolutionRenamed ()) {
						executeCommands.Add (new CreateUpgradeJobCommand (this));
						executeCommands.Add (new WaitForJobCompletionCommand (this, res.WaitUpgrade));
					} else {
						executeCommands.Add (new CreateRetractionJobCommand (this));
						executeCommands.Add (new WaitForJobCompletionCommand (this, res.WaitRetract));
						executeCommands.Add (new RemoveSolutionCommand (this));
						executeCommands.Add (new AddSolutionCommand (this));
						executeCommands.Add (new CreateDeploymentJobCommand (this, GetDeployedApplications ()));
						executeCommands.Add (new WaitForJobCompletionCommand (this, res.WaitDeploy));
					}
					if (featureScope == SPFeatureScope.Farm) {
						executeCommands.Add (new ActivateFarmFeatureCommand (this));
					}
					if (featureScope == SPFeatureScope.Site) {
						executeCommands.Add (new ActivateSiteCollectionFeatureCommand (this, deactivateSiteCollectionFeatureCommand.DeactivatedSiteCollections));
					}
					executeCommands.Add (new RegisterVersionNumberCommand (this));
					break;

				case InstallOperation.Repair:
					if (featureScope == SPFeatureScope.Farm) {
						executeCommands.Add (new DeactivateFarmFeatureCommand (this));
					}
					if (featureScope == SPFeatureScope.Site) {
						deactivateSiteCollectionFeatureCommand = new DeactivateSiteCollectionFeatureCommand (this);
						executeCommands.Add (deactivateSiteCollectionFeatureCommand);
					}
					executeCommands.Add (new CreateRetractionJobCommand (this));
					executeCommands.Add (new WaitForJobCompletionCommand (this, res.WaitRetract));
					executeCommands.Add (new RemoveSolutionCommand (this));
					executeCommands.Add (new AddSolutionCommand (this));
					executeCommands.Add (new CreateDeploymentJobCommand (this, GetDeployedApplications ()));
					executeCommands.Add (new WaitForJobCompletionCommand (this, res.WaitDeploy));
					if (featureScope == SPFeatureScope.Farm) {
						executeCommands.Add (new ActivateFarmFeatureCommand (this));
					}
					if (featureScope == SPFeatureScope.Site) {
						executeCommands.Add (new ActivateSiteCollectionFeatureCommand (this, deactivateSiteCollectionFeatureCommand.DeactivatedSiteCollections));
					}
					executeCommands.Add (new RegisterVersionNumberCommand (this));
					break;

				case InstallOperation.Uninstall:
					if (featureScope == SPFeatureScope.Farm) {
						executeCommands.Add (new DeactivateFarmFeatureCommand (this));
					}
					if (featureScope == SPFeatureScope.Site) {
						executeCommands.Add (new DeactivateSiteCollectionFeatureCommand (this));
					}
					executeCommands.Add (new CreateRetractionJobCommand (this));
					executeCommands.Add (new WaitForJobCompletionCommand (this, res.WaitRetract));
					executeCommands.Add (new RemoveSolutionCommand (this));
					executeCommands.Add (new UnregisterVersionNumberCommand (this));
					break;
			}

			progressBar.Maximum = executeCommands.Count;

			descriptionLabel.Text = executeCommands [0].Description;

			timer.Interval = 1000;
			timer.Tick += new EventHandler (TimerEventInstall);
			timer.Start ();
		}

		#endregion

		#region Private Methods

		private void HandleCompletion () {
			Dictionary<string, object> obj = new Dictionary<string, object> ();
			completed = true;

			obj ["t"] = DateTime.Now.Ticks;
			if (errors == 0)
				try {
					
					ProductPage.UpdateStatus (obj, false, true, false, res.ResourceManager.GetString ("map_" + ProductPage.GuidLower (InstallConfiguration.SolutionId)), new List<string> (res.ResourceManager.GetString ("pid_" + ProductPage.GuidLower (InstallConfiguration.SolutionId)).Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ConvertAll<byte> (delegate (string s) {
						return byte.Parse (s.Trim ());
					}).ToArray ());
				} catch (Exception ex) {
					MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}

			Form.NextButton.Enabled = true;
			Form.AbortButton.Text = res.AbortButton;
			Form.AbortButton.Enabled = false;

			CompletionControl nextControl = new CompletionControl ();

			foreach (string message in log.Messages) {
				nextControl.Details += message + "\r\n";
			}

			switch (Form.Operation) {
				case InstallOperation.Install:
					nextControl.Title = errors == 0 ? res.FinSuccInstall : res.FinFailInstall;
					break;

				case InstallOperation.Upgrade:
					nextControl.Title = errors == 0 ? res.FinSuccUpgrade : res.FinFailUpgrade;
					break;

				case InstallOperation.Repair:
					nextControl.Title = errors == 0 ? res.FinSuccRepair : res.FinFailRepair;
					break;

				case InstallOperation.Uninstall:
					nextControl.Title = errors == 0 ? res.FinSuccUninstall : res.FinFailUninstall;
					break;
			}

			Form.ContentControls.Add (nextControl);
		}

		private void InitiateRollback () {
			Form.AbortButton.Enabled = false;

			progressBar.Maximum = rollbackCommands.Count;
			progressBar.Value = rollbackCommands.Count;
			nextCommand = 0;
			rollbackErrors = 0;
			progressBar.Step = -1;

			//
			// Create and start new timer.
			//
			timer = new System.Windows.Forms.Timer ();
			timer.Interval = 1000;
			timer.Tick += new EventHandler (TimerEventRollback);
			timer.Start ();
		}

		private bool IsSolutionRenamed () {
			SPFarm farm = SPFarm.Local;
			SPSolution solution = farm.Solutions [InstallConfiguration.SolutionId];
			if (solution == null)
				return false;

			FileInfo solutionFileInfo = new FileInfo (InstallConfiguration.SolutionFile);

			return !solution.Name.Equals (solutionFileInfo.Name, StringComparison.OrdinalIgnoreCase);
		}

		private Collection<SPWebApplication> GetDeployedApplications () {
			SPFarm farm = SPFarm.Local;
			SPSolution solution = farm.Solutions [InstallConfiguration.SolutionId];
			if (solution.ContainsWebApplicationResource) {
				return solution.DeployedWebApplications;
			}
			return null;
		}

		#endregion

		#region Command Classes

		/// <summary>
		/// The base class of all installation commands.
		/// </summary>
		private abstract class Command {
			private readonly InstallProcessControl parent;

			protected Command (InstallProcessControl parent) {
				this.parent = parent;
			}

			internal InstallProcessControl Parent {
				get {
					return parent;
				}
			}

			internal abstract string Description {
				get;
			}

			protected internal virtual bool Execute () {
				return true;
			}

			protected internal virtual bool Rollback () {
				return true;
			}
		}

		private class CommandList : List<Command> {
		}

		/// <summary>
		/// The base class of all SharePoint solution related commands.
		/// </summary>
		private abstract class SolutionCommand : Command {
			protected SolutionCommand (InstallProcessControl parent) : base (parent) {
			}

			protected void RemoveSolution () {
				try {
					SPFarm farm = SPFarm.Local;
					SPSolution solution = farm.Solutions [InstallConfiguration.SolutionId];
					if (solution != null) {
						if (!solution.Deployed) {
							solution.Delete ();
						}
					}
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}
		}

		/// <summary>
		/// Command for adding the SharePoint solution.
		/// </summary>
		private class AddSolutionCommand : SolutionCommand {
			internal AddSolutionCommand (InstallProcessControl parent)
				: base (parent) {
			}

			internal override string Description {
				get {
					return res.AddSolution;
				}
			}

			protected internal override bool Execute () {
				string filename = InstallConfiguration.SolutionFile;
				if (String.IsNullOrEmpty (filename)) {
					throw new InstallException (res.ConfigErrorNoSolution);
				}
#if DEBUG
				if (!File.Exists (filename))
					return true;
#endif
				try {
					SPFarm farm = SPFarm.Local;
					SPSolution solution = farm.Solutions.Add (filename);
					return true;
				} catch (IOException ex) {
					throw new InstallException (ex.Message, ex);
				} catch (ArgumentException ex) {
					throw new InstallException (ex.Message, ex);
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}

			protected internal override bool Rollback () {
				RemoveSolution ();
				return true;
			}
		}

		/// <summary>
		/// Command for removing the SharePoint solution.
		/// </summary>
		private class RemoveSolutionCommand : SolutionCommand {
			internal RemoveSolutionCommand (InstallProcessControl parent) : base (parent) {
			}

			internal override string Description {
				get {
					return res.RemoveSolution;
				}
			}

			protected internal override bool Execute () {
				RemoveSolution ();
				return true;
			}
		}

		private abstract class JobCommand : Command {
			protected JobCommand (InstallProcessControl parent) : base (parent) {
			}

			protected static void RemoveExistingJob (SPSolution solution) {
				if (solution.JobStatus == SPRunningJobStatus.Initialized) {
					throw new InstallException (res.DoubleDeployment);
				}

				SPJobDefinition jobDefinition = GetSolutionJob (solution);
				if (jobDefinition != null) {
					jobDefinition.Delete ();
					Thread.Sleep (500);
				}
			}

			private static SPJobDefinition GetSolutionJob (SPSolution solution) {
				SPFarm localFarm = SPFarm.Local;
				SPTimerService service = localFarm.TimerService;
				foreach (SPJobDefinition definition in service.JobDefinitions) {
					if (definition.Title != null && definition.Title.Contains (solution.Name)) {
						return definition;
					}
				}
				return null;
			}

			protected static DateTime GetImmediateJobTime () {
				return DateTime.Now - TimeSpan.FromDays (1);
			}
		}

		/// <summary>
		/// Command for creating a deployment job.
		/// </summary>
		private class CreateDeploymentJobCommand : JobCommand {
			private readonly Collection<SPWebApplication> applications;

			internal CreateDeploymentJobCommand (InstallProcessControl parent, ICollection applications)
				: base (parent) {
				if (applications != null) {
					this.applications = new Collection<SPWebApplication> ();
					foreach (SPWebApplication application in applications) {
						this.applications.Add (application);
					}
				} else {
					this.applications = null;
				}
			}

			internal override string Description {
				get {
					return res.DeploySolution;
				}
			}

			protected internal override bool Execute () {
				try {
					SPSolution installedSolution = SPFarm.Local.Solutions [InstallConfiguration.SolutionId];
					if(installedSolution==null)
#if DEBUG
						return true;
#else
						return false;
#endif
					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists) {
						RemoveExistingJob (installedSolution);
					}

					log.Info (res.SolutionDeployment);
					if (installedSolution.ContainsWebApplicationResource && applications != null && applications.Count > 0) {
						installedSolution.Deploy (GetImmediateJobTime (), true, applications, true);
					} else {
						installedSolution.Deploy (GetImmediateJobTime (), true, true);
					}

					return true;
				} catch (SPException ex) {
					throw new InstallException (ex.Message, ex);
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}

			protected internal override bool Rollback () {
				SPSolution installedSolution = SPFarm.Local.Solutions [InstallConfiguration.SolutionId];

				if (installedSolution != null) {
					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists) {
						RemoveExistingJob (installedSolution);
					}

					log.Info (res.SolutionRetraction);
					if (installedSolution.ContainsWebApplicationResource) {
						installedSolution.Retract (GetImmediateJobTime (), applications);
					} else {
						installedSolution.Retract (GetImmediateJobTime ());
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Command for creating an upgrade job.
		/// </summary>
		private class CreateUpgradeJobCommand : JobCommand {
			internal CreateUpgradeJobCommand (InstallProcessControl parent)
				: base (parent) {
			}

			internal override string Description {
				get {
					return res.UpgradeSolution;
				}
			}

			protected internal override bool Execute () {
				try {
					string filename = InstallConfiguration.SolutionFile;
					if (String.IsNullOrEmpty (filename)) {
						throw new InstallException (res.ConfigErrorNoSolution);
					}

					SPSolution installedSolution = SPFarm.Local.Solutions [InstallConfiguration.SolutionId];

					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists) {
						RemoveExistingJob (installedSolution);
					}

					log.Info (res.SolutionUpgrade);
					installedSolution.Upgrade (filename, GetImmediateJobTime ());
					return true;
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}
		}

		/// <summary>
		/// Command for creating a retraction job.
		/// </summary>
		private class CreateRetractionJobCommand : JobCommand {
			internal CreateRetractionJobCommand (InstallProcessControl parent)
				: base (parent) {
			}

			internal override string Description {
				get {
					return res.RetractSolution;
				}
			}

			protected internal override bool Execute () {
				try {
					SPSolution installedSolution = SPFarm.Local.Solutions [InstallConfiguration.SolutionId];

					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists) {
						RemoveExistingJob (installedSolution);
					}

					if (installedSolution.Deployed) {
						log.Info (res.SolutionRetraction);
						if (installedSolution.ContainsWebApplicationResource) {
							Collection<SPWebApplication> applications = installedSolution.DeployedWebApplications;
							installedSolution.Retract (GetImmediateJobTime (), applications);
						} else {
							installedSolution.Retract (GetImmediateJobTime ());
						}
					}
					return true;
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}
		}


		private class WaitForJobCompletionCommand : Command {
			private readonly string description;
			private DateTime startTime;
			private bool first = true;

			internal WaitForJobCompletionCommand (InstallProcessControl parent, string description)
				: base (parent) {
				this.description = description;
			}

			internal override string Description {
				get {
					return description;
				}
			}

			protected internal override bool Execute () {
				try {
					SPSolution installedSolution = SPFarm.Local.Solutions [InstallConfiguration.SolutionId];
					if(installedSolution==null)
#if DEBUG
						return true;
#else
						return false;
#endif
					if (first) {
						if (!installedSolution.JobExists)
							return true;
						startTime = DateTime.Now;
						first = false;
					}

					//
					// Wait for job to end
					//
					if (installedSolution.JobExists) {
						if (DateTime.Now > startTime.Add (JobTimeout)) {
							throw new InstallException (res.TimeoutError);
						}

						return false;
					} else {
						log.Info (installedSolution.LastOperationDetails);

						SPSolutionOperationResult result = installedSolution.LastOperationResult;
						if (result != SPSolutionOperationResult.DeploymentSucceeded && result != SPSolutionOperationResult.RetractionSucceeded) {
							throw new InstallException (installedSolution.LastOperationDetails);
						}

						return true;
					}
				} catch (Exception ex) {
					throw new InstallException (ex.Message, ex);
				}
			}

			protected internal override bool Rollback () {
				SPSolution installedSolution = SPFarm.Local.Solutions [InstallConfiguration.SolutionId];

				//
				// Wait for job to end
				//
				if (installedSolution != null) {
					if (installedSolution.JobExists) {
						if (DateTime.Now > startTime.Add (JobTimeout)) {
							throw new InstallException (res.TimeoutError);
						}
						return false;
					} else {
						log.Info (installedSolution.LastOperationDetails);
					}
				}

				return true;
			}
		}

		private abstract class FeatureCommand : Command {
			protected FeatureCommand (InstallProcessControl parent) : base (parent) {
			}

			// Modif JPI - Début
			protected static void DeactivateFeature (List<Guid?> featureIds) {
				try {
					if (featureIds != null && featureIds.Count > 0) {
						foreach (Guid? featureId in featureIds) {
							if (featureId != null) {
								SPFeature feature = SPWebService.AdministrationService.Features [featureId.Value];
								if (feature != null) {
									SPWebService.AdministrationService.Features.Remove (featureId.Value);
								}
							}
						}
					}
				} catch (ArgumentException ex)  // Missing assembly in GAC
				{
					log.Warn (ex.Message, ex);
				} catch (InvalidOperationException ex)  // Missing receiver class
				{
					log.Warn (ex.Message, ex);
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}
			// Modif JPI - Fin
		}

		private class ActivateFarmFeatureCommand : FeatureCommand {
			internal ActivateFarmFeatureCommand (InstallProcessControl parent) : base (parent) {
			}

			internal override string Description {
				get {
					// Modif JPI - Début
					return res.ActivateFarmFeatures;
					// Modif JPI - Fin
				}
			}

			protected internal override bool Execute () {
				try {
					// Modif JPI - Début
					List<Guid?> featureIds = InstallConfiguration.FeatureId;
					if (featureIds != null && featureIds.Count > 0) {
						foreach (Guid? featureId in featureIds) {
							if (featureId != null) {
								SPFeature feature = SPWebService.AdministrationService.Features.Add (featureId.Value, true);
							}
						}
					}
					return true;
					// Modif JPI - Fin
				} catch (Exception ex) {
					throw new InstallException (ex.Message, ex);
				}
			}

			protected internal override bool Rollback () {
				DeactivateFeature (InstallConfiguration.FeatureId);
				return true;
			}
		}

		private class DeactivateFarmFeatureCommand : FeatureCommand {
			internal DeactivateFarmFeatureCommand (InstallProcessControl parent) : base (parent) {
			}

			internal override string Description {
				get {
					// Modif JPI - Début
					return res.DeactivateFarmFeatures;
					// Modif JPI - Fin
				}
			}

			protected internal override bool Execute () {
				try {
					// Modif JPI - Début
					List<Guid?> featureIds = InstallConfiguration.FeatureId;
					if (featureIds != null && featureIds.Count > 0) {
						foreach (Guid? featureId in featureIds) {
							if (featureId != null && SPWebService.AdministrationService.Features [featureId.Value] != null) {
								SPWebService.AdministrationService.Features.Remove (featureId.Value);
							}
						}
					}

					return true;
					// Modif JPI - Fin
				} catch (Exception ex) {
					log.Error (ex.Message, ex);
				}

				return true;
			}
		}

		private abstract class SiteCollectionFeatureCommand : Command {
			internal SiteCollectionFeatureCommand (InstallProcessControl parent) : base (parent) {
			}

			// Modif JPI - Début
			protected static void DeactivateFeature (List<SPSite> siteCollections, List<Guid?> featureIds) {
				try {
					if (siteCollections != null && featureIds != null && featureIds.Count > 0) {
						log.Info (res.FeatureDeactivation);
						foreach (SPSite siteCollection in siteCollections) {
							foreach (Guid? featureId in featureIds) {
								if (featureId == null)
									continue;

								SPFeature feature = siteCollection.Features [featureId.Value];
								if (feature == null)
									continue;

								siteCollection.Features.Remove (featureId.Value);
								log.Info (siteCollection.Url + " : " + featureId.Value.ToString ());
							}
						}
					}
				} catch (ArgumentException ex)  // Missing assembly in GAC
				{
					log.Warn (ex.Message, ex);
				} catch (InvalidOperationException ex)  // Missing receiver class
				{
					log.Warn (ex.Message, ex);
				} catch (SqlException ex) {
					throw new InstallException (ex.Message, ex);
				}
			}
			// Modif JPI - Fin
		}

		private class ActivateSiteCollectionFeatureCommand : SiteCollectionFeatureCommand {
			private readonly List<SPSite> siteCollections;

			internal ActivateSiteCollectionFeatureCommand (InstallProcessControl parent, List<SPSite> siteCollections)
				: base (parent) {
				this.siteCollections = siteCollections;
			}

			internal override string Description {
				// Modif JPI - Début
				get {
					return String.Format (res.ActivateSiteFeatures, siteCollections.Count);
				}
				// Modif JPI - Fin
			}

			protected internal override bool Execute () {
				try {
					// Modif JPI - Début
					List<Guid?> featureIds = InstallConfiguration.FeatureId;
					if (siteCollections != null && featureIds != null && featureIds.Count > 0) {
						log.Info (res.FeatureActivation);
						foreach (SPSite siteCollection in siteCollections) {
							foreach (Guid? featureId in featureIds) {
								if (featureId == null)
									continue;
								SPFeature feature = siteCollection.Features.Add (featureId.Value, true);
								log.Info (siteCollection.Url + " : " + featureId.Value.ToString ());
							}
						}
					}
					return true;
					// Modif JPI - Fin
				} catch (Exception ex) {
					log.Warn (ex.Message, ex);
					return false;
				}
			}

			protected internal override bool Rollback () {
				DeactivateFeature (siteCollections, InstallConfiguration.FeatureId);
				return true;
			}
		}

		private class DeactivateSiteCollectionFeatureCommand : SiteCollectionFeatureCommand {
			private List<SPSite> deactivatedSiteCollections;

			internal DeactivateSiteCollectionFeatureCommand (InstallProcessControl parent)
				: base (parent) {
				deactivatedSiteCollections = new List<SPSite> ();
			}

			public List<SPSite> DeactivatedSiteCollections {
				get {
					return deactivatedSiteCollections;
				}
			}

			internal override string Description {
				get {
					return res.DeactivateSiteFeatures;
				}
			}

			protected internal override bool Execute () {
				try {
					List<Guid?> featureIds = InstallConfiguration.FeatureId;

					SPFarm farm = SPFarm.Local;
					SPSolution solution = farm.Solutions [InstallConfiguration.SolutionId];
					if (solution != null && solution.Deployed && featureIds != null && featureIds.Count > 0) {
						log.Info (res.FeatureDeactivation);

						//
						// LFN - Stopped using solution.DeployedWebApplications as it seems to produced a FormatException 
						// when created a new Guid value. Looks like a bug in SharePoint that we cannot do anything about. 
						// I have therefore adopted a new strategy by looping through all Web applications.
						//

						foreach (SPWebApplication webApp in SPWebService.AdministrationService.WebApplications) {
							DeactivateFeatures (webApp);
						}

						foreach (SPWebApplication webApp in SPWebService.ContentService.WebApplications) {
							DeactivateFeatures (webApp);
						}

						// KML - not sure why JPI didn't just do this above and avoided using the local dictionary
						// LFN - Agree, local dictionary removed making the following code obsolete.
						/*
						foreach (SPSite _spSite in _dicDeactivatedSiteCollections.Values)
						{
							deactivatedSiteCollections.Add(_spSite);
						}
						DeactivateFeature(deactivatedSiteCollections, featureIds);
						*/
					}
				} catch (Exception ex) {
					log.Error (ex.Message, ex);
				}

				return true;
			}

			private void DeactivateFeatures (SPWebApplication webApp) {
				List<Guid?> featureIds = InstallConfiguration.FeatureId;

				foreach (SPSite siteCollection in webApp.Sites) {
					foreach (Guid? featureId in featureIds) {
						if (featureId == null)
							continue;
						if (siteCollection.Features [featureId.Value] == null)
							continue;

						log.Info (siteCollection.Url + " : " + featureId.Value.ToString ());

						// LFN - Just deactivate the feature right away. No need to use intermidate step with local dictionary.
						try {
							siteCollection.Features.Remove (featureId.Value);
						} catch (UnauthorizedAccessException) {
							try {
								SPSecurity.RunWithElevatedPrivileges (delegate () {
									using (SPSite site = new SPSite (siteCollection.ID))
										site.Features.Remove (featureId.Value);
								});
							} catch {
							}
						} catch {
						}

						// KML - not sure why JPI used this local dictionary
						//       instead of doing "deactivatedSiteCollections.Add(siteCollection)" here
						// LFN - Agree no need to do this. Works just fine by deactivating directly.
						//_dicDeactivatedSiteCollections[siteCollection.Url] = siteCollection;
					}

					// LFN - It is a memory and resource leak to forget this! See http://msdn.microsoft.com/en-us/library/aa973248.aspx
					// LFN - Well, they might be disposed by the system when the installer process dies. But I think it is good coding
					// practice never to forget this. 
					siteCollection.Dispose ();
				}
			}
		}

		/// <summary>
		/// Command that registers the version number of a solution.
		/// </summary>
		private class RegisterVersionNumberCommand : Command {
			private Version oldVersion;

			internal RegisterVersionNumberCommand (InstallProcessControl parent) : base (parent) {
			}

			internal override string Description {
				get {
					return res.RegisterVersion;
				}
			}

			protected internal override bool Execute () {
				oldVersion = InstallConfiguration.InstalledVersion;
				InstallConfiguration.InstalledVersion = InstallConfiguration.SolutionVersion;
				return true;
			}

			protected internal override bool Rollback () {
				InstallConfiguration.InstalledVersion = oldVersion;
				return true;
			}
		}

		/// <summary>
		/// Command that unregisters the version number of a solution.
		/// </summary>
		private class UnregisterVersionNumberCommand : Command {
			internal UnregisterVersionNumberCommand (InstallProcessControl parent) : base (parent) {
			}

			internal override string Description {
				get {
					return res.UnregisterVersion;
				}
			}

			protected internal override bool Execute () {
				InstallConfiguration.InstalledVersion = null;
				return true;
			}
		}

		#endregion

		#region ILog Wrapper

		private class MessageList : List<string> {
		}

		private class MessageCollector : ILog {

			private readonly ILog wrappee;
			private readonly MessageList messages = new MessageList ();

			internal MessageCollector (ILog wrappee) {
				this.wrappee = wrappee;
			}

			public MessageList Messages {
				get {
					return messages;
				}
			}

			public void Info (object message) {
				messages.Add (message.ToString ());
				wrappee.Info (message);
			}

			public void Info (object message, Exception t) {
				messages.Add (message.ToString ());
				messages.Add (t.ToString ());
				wrappee.Info (message, t);
			}

			public void Warn (object message) {
				wrappee.Warn (message);
			}

			public void Warn (object message, Exception t) {
				wrappee.Warn (message, t);
			}

			public void Error (object message) {
				messages.Add (message.ToString ());
				wrappee.Error (message);
			}

			public void Error (object message, Exception t) {
				messages.Add (message.ToString ());
				messages.Add (t.ToString ());
				wrappee.Error (message, t);
			}

			public void Fatal (object message) {
				wrappee.Fatal (message);
			}

			public void Fatal (object message, Exception t) {
				wrappee.Fatal (message, t);
			}
		}

		#endregion

	}

}
