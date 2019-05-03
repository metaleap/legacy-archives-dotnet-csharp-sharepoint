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
/* KML: Added WebApplicationTargets and SitecollectionTargets     */
/*                                                                */
/******************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;


namespace roxority_SetupZen
{
  public class InstallOptions
  {
    // KML TODO - get rid of generic targets - talk to Lars first
    private readonly IList targets;
    private readonly List<SPWebApplication> webApplicationTargets;
    private readonly List<SPSite> siteCollectionTargets;

    public InstallOptions()
    {
      this.targets = new ArrayList();
      this.webApplicationTargets = new List<SPWebApplication>();
      this.siteCollectionTargets = new List<SPSite>();
    }

    public IList Targets
    {
      get { return targets; }
    }

    public List<SPWebApplication> WebApplicationTargets
    {
      get { return webApplicationTargets; }
    }

    public List<SPSite> SiteCollectionTargets
    {
      get { return siteCollectionTargets; }
    }
  }
}
