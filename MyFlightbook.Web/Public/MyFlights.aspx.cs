﻿using MyFlightbook.Encryptors;
using MyFlightbook.FlightStatistics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2011-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.PublicPages
{
    public partial class MyFlights : Page
    {
        #region Infinite Scroll support
        public static LogbookEntry[] GetFlights(string szUser, int skip, int pageSize)
        {
            return LogbookEntryBase.GetPublicFlightsForUser(szUser, skip, pageSize);
        }


        [WebMethod()]
        public static FlightRow[] HtmlRowsForFlights(string szUser, int skip, int pageSize)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = util.SessionCulture ?? CultureInfo.CurrentCulture;
            // We have no Page, so things like Page_Load don't get called.
            // We fix this by faking a page and calling Server.Execute on it.  This sets up the form and - more importantly - causes Page_load to be called on loaded controls.
            using (Page p = new FormlessPage())
            {
                p.Controls.Add(new HtmlForm());
                using (StringWriter sw = new StringWriter(CultureInfo.CurrentCulture))
                    HttpContext.Current.Server.Execute(p, sw, false);

                IEnumerable<LogbookEntry> rgle = Array.Empty<LogbookEntry>();
                if (String.IsNullOrEmpty(szUser))
                {
                    List<LogbookEntry> lst = new List<LogbookEntry>(FlightStats.GetFlightStats().RecentPublicFlights);
                    if (skip > 0)
                        lst.RemoveRange(0, Math.Min(skip, lst.Count));
                    if (lst.Count > pageSize)
                        lst.RemoveRange(pageSize, lst.Count - pageSize);
                    rgle = lst;
                }
                else
                    rgle = GetFlights(szUser, skip, pageSize);

                List<FlightRow> lstRows = new List<FlightRow>();
                foreach (LogbookEntry le in rgle)
                {
                    Controls_mfbPublicFlightItem pfe = (Controls_mfbPublicFlightItem)p.LoadControl("~/Controls/mfbPublicFlightItem.ascx");
                    p.Form.Controls.Add(pfe);

                    pfe.Entry = le;

                    // Now, write it out.
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    using (StringWriter sw = new StringWriter(sb, CultureInfo.CurrentCulture))
                    {
                        using (HtmlTextWriter htmlTW = new HtmlTextWriter(sw))
                            pfe.RenderControl(htmlTW);
                    }

                    lstRows.Add(new FlightRow() { HTMLRowText = sb.ToString(), FBDivID = string.Empty });
                }

                return lstRows.ToArray();
            }
        }
        #endregion

        #region Properties
        protected int PageSize { get; set; }
        protected string UserName { get; set; }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.SelectedTab = tabID.tabHome;
            PageSize = 15;

            // figure out who to show
            if (!IsPostBack)
            {
                IEnumerable<LogbookEntry> rgle = Array.Empty<LogbookEntry>();

                string szUserEnc = util.GetStringParam(Request, "uid");
                UserName = string.Empty;

                if (!String.IsNullOrEmpty(szUserEnc))
                {
                    SharedDataEncryptor enc = new SharedDataEncryptor(MFBConstants.keyEncryptMyFlights);
                    UserName = enc.Decrypt(szUserEnc);
                }

                if (String.IsNullOrEmpty(UserName))
                {
                    FlightStats fs = FlightStats.GetFlightStats();
                    List<LogbookEntry> lst = new List<LogbookEntry>(fs.RecentPublicFlights);
                    if (lst.Count > PageSize)
                        lst.RemoveRange(PageSize, lst.Count - PageSize);
                    rgle = lst;
                }
                else
                {
                    try
                    {
                        // below can throw argument null exception if it's an invalid username
                        Profile pf = Profile.GetUser(UserName);
                        if (pf.UserFullName.Length > 0)
                            lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.PublicFlightPageHeader, HttpUtility.HtmlEncode(pf.UserFullName));
                        rgle = LogbookEntryBase.GetPublicFlightsForUser(UserName, 0, PageSize);
                    }
                    catch (Exception ex) when (ex is NullReferenceException) { }
                }

                gvMyFlights.DataSource = rgle;
                gvMyFlights.DataBind();
            }

        }

        public void gvMyFlights_rowDataBound(Object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
                ((Controls_mfbPublicFlightItem)e.Row.FindControl("mfbPublicFlightItem1")).Entry = (LogbookEntry)e.Row.DataItem;
        }
    }
}