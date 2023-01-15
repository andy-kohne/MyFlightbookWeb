﻿using MyFlightbook.Templates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls.FlightEditing
{
    public partial class mfbEditPropSet : UserControl
    {
        private const string vsActiveProps = "vsActiveProps";
        private const string vsPropVals = "vsPropVals";
        private const string vsPropTemplates = "vsPropTemplates";
        private const string vsPropUser = "vsPropUser";

        public event EventHandler<CrossFillDescriptorArgs> CrossFillDescriptorRequested;

        #region properties
        /// <summary>
        /// The username for whom we are showing properties and templates.  If unspecified, currently logged user is used.
        /// </summary>
        public string Username
        {
            get { return ((string)ViewState[vsPropUser]) ?? Page.User.Identity.Name; }
            set { ViewState[vsPropUser] = value; }
        }

        private List<CustomFlightProperty> m_cfpActive;
        /// <summary>
        /// These are the properties that are instantiated to edit
        /// </summary>
        private List<CustomFlightProperty> ActiveProperties
        {
            get
            {
                if (m_cfpActive == null)
                    m_cfpActive = new List<CustomFlightProperty>();
                return m_cfpActive;
            }
        }

        // Note: On postbacks initiated within EditFlight, any properties that were added to the list but not given values are dropped.  
        // This is because the editflight form is distilling the list before initiating the postback.
        /// <summary>
        /// A set of the property types being actively edited.
        /// </summary>
        private List<int> ActivePropTypes
        {
            get
            {
                if (ViewState[vsActiveProps] == null)
                    ViewState[vsActiveProps] = new List<int>();
                return (List<int>)ViewState[vsActiveProps];
            }
        }

        /// <summary>
        /// Maps existing properties to IDs based on the proptypeid.
        /// </summary>
        private Dictionary<int, int> ExistingPropIDs
        {
            get
            {
                if (ViewState[vsPropVals] == null)
                    ViewState[vsPropVals] = new Dictionary<int, int>();
                return (Dictionary<int, int>)ViewState[vsPropVals];
            }
        }

        protected HashSet<PropertyTemplate> ActiveTemplates
        {
            get
            {
                if (ViewState[vsPropTemplates] == null)
                {
                    // Initialize the hashset with any default property templates; if none found, use the MRU template
                    HashSet<PropertyTemplate> hs = new HashSet<PropertyTemplate>();
                    if (Page.User.Identity.IsAuthenticated)
                    {
                        IEnumerable<PropertyTemplate> rgpt = UserPropertyTemplate.TemplatesForUser(Username);
                        foreach (PropertyTemplate pt in rgpt)
                            if (pt.IsDefault)
                                hs.Add(pt);

                        if (hs.Count == 0)
                            hs.Add(new MRUPropertyTemplate(Username));
                    }
                    mfbSelectTemplates.AddTemplates(hs);
                    ViewState[vsPropTemplates] = hs;
                }
                return (HashSet<PropertyTemplate>)ViewState[vsPropTemplates];
            }
        }

        /// <summary>
        /// The properties that we want to ensure we have.  E.g., from a flight.
        /// </summary>
        private List<CustomFlightProperty> Properties { get; set; }

        private List<CustomFlightProperty> m_propertiesFromPropSet;

        /// <summary>
        /// Gets/sets a list of properties to edit.
        /// When getting, this returns ALL ACTIVE properties; it needs to be distilled:
        ///  - Delete any property that has a default value
        ///  - Update or add the remainders.
        /// </summary>
        private List<CustomFlightProperty> PropertiesFromPropSet
        {
            get
            {
                if (m_propertiesFromPropSet == null)
                {
                    m_propertiesFromPropSet = new List<CustomFlightProperty>();
                    foreach (mfbEditProp ep in plcHolderProps.Controls)
                        m_propertiesFromPropSet.Add(ep.FlightProperty);
                }
                return m_propertiesFromPropSet;
            }
        }

        public void AddTemplate(PropertyTemplate pt)
        {
            if (pt == null)
                throw new ArgumentNullException(nameof(pt));

            ActiveTemplates.Add(pt);
            mfbSelectTemplates.AddTemplate(pt.ID);
        }

        public void AddTemplates(IEnumerable<PropertyTemplate> rgpt)
        {
            if (rgpt == null)
                throw new ArgumentNullException(nameof(rgpt));
            foreach (PropertyTemplate pt in rgpt)
                ActiveTemplates.Add(pt);
            mfbSelectTemplates.AddTemplates(rgpt);
        }

        public void RemoveTemplate(int id)
        {
            ActiveTemplates.RemoveWhere(pt => pt.ID == id);
            mfbSelectTemplates.RemoveTemplate(id);
        }

        public void RemoveAllTemplates()
        {
            ActiveTemplates.Clear();
            mfbSelectTemplates.RemoveAllTemplates();
        }

        public void SetFlightProperties(IEnumerable<CustomFlightProperty> rgcfp)
        {
            if (rgcfp == null)
                return;
            Properties = new List<CustomFlightProperty>(rgcfp);
            foreach (CustomFlightProperty cfp in rgcfp)
                if (!cfp.IsDefaultValue && cfp.IsExisting)
                    ExistingPropIDs[cfp.PropTypeID] = cfp.PropID;
            SegregateProperties();              // add the new property to the list
            PopulateControls();                 // And re-populate.
        }

        /// <summary>
        /// Returns a DISTILLED set of properties.  Specifically, every property that is a default value is removed (and, if it is an existing property, it is deleted as well).
        /// </summary>
        public IEnumerable<CustomFlightProperty> DistilledList
        {
            get
            {
                List<CustomFlightProperty> lst = PropertiesFromPropSet;
                bool fPropsDeleted = false;
                lst.ForEach((cfp) =>
                {
                    if (cfp.IsDefaultValue && cfp.IsExisting)
                    {
                        cfp.DeleteProperty();
                        ExistingPropIDs.Remove(cfp.PropTypeID);
                        fPropsDeleted = true;
                    }
                });

                if (fPropsDeleted)
                    Profile.GetUser(Username).SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);

                lst.RemoveAll(cfp => cfp.IsDefaultValue);
                return lst;
            }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            RecreateControls(); // set them up for postback or initial
            Page.ClientScript.RegisterClientScriptInclude("filterDropdown", ResolveClientUrl("~/Public/Scripts/DropDownFilter.js?v=2"));
            txtFilter.Attributes["onkeyup"] = String.Format(CultureInfo.InvariantCulture, "FilterItems(this, '{0}', '{1}', '{2}')", cmbPropsToAdd.ClientID, lblFilterMessage.ClientID, Resources.LogbookEntry.PropertiesFound);
            imgSearch.Attributes["onclick"] = "javascript:toggleSearchBox();";
        }

        protected void SegregateProperties(bool fStripDefault = false)
        {
            List<CustomPropertyType> lstRemainingProps = new List<CustomPropertyType>();

            ActiveProperties.Clear();
            ActivePropTypes.Clear();

            PropertyTemplate ptMerged = PropertyTemplate.MergedTemplate(ActiveTemplates);

            // this is cached so we can do it on every call, postback or not
            CustomPropertyType[] rgCptAll = CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.IsAuthenticated ? Username : string.Empty);

            foreach (CustomPropertyType cpt in rgCptAll)
            {
                // see if this either has a value or is in one of the active templates.
                // if it doesn't have a value but is in a template, give it a value.
                CustomFlightProperty fp = Properties.Find(cfp => cfp.PropTypeID == cpt.PropTypeID);

                // To be included, it must be EITHER
                // a) in the merged set of templates OR
                // b) in the set of properties with a non-default value (fp != null && !fp.IsDefaultValue) OR
                // c) in the set of properties with a default value (fp != null && (!fStripDefault && fp.IsDefaultValue)
                bool fInclude = ptMerged.ContainsProperty(cpt.PropTypeID) || (fp != null && (!fStripDefault || !fp.IsDefaultValue));
                if (fp == null)
                    fp = new CustomFlightProperty(cpt);

                if (!fInclude)
                    lstRemainingProps.Add(cpt);
                else
                {
                    ActiveProperties.Add(fp);
                    ActivePropTypes.Add(fp.PropTypeID);
                }
            }

            ActiveProperties.Sort((cfp1, cfp2) => { return cfp1.PropertyType.SortKey.CompareCurrentCultureIgnoreCase(cfp2.PropertyType.SortKey); });

            ListItem li = cmbPropsToAdd.Items[0];
            cmbPropsToAdd.Items.Clear();
            cmbPropsToAdd.Items.Add(li);
            cmbPropsToAdd.SelectedValue = string.Empty;    // reset the selection
            cmbPropsToAdd.DataSource = lstRemainingProps;
            cmbPropsToAdd.DataBind();
        }

        private static string IDForPropType(CustomPropertyType cpt)
        {
            return String.Format(CultureInfo.InvariantCulture, "editProp{0}", cpt.PropTypeID);
        }

        private void InsertEditProp(CustomFlightProperty cfp)
        {
            mfbEditProp ep = (mfbEditProp)LoadControl("~/Controls/mfbEditProp.ascx");
            // Add it to the placeholder so that the client ID works, then set the client ID before setting the property so that it picks up cross-fill
            plcHolderProps.Controls.Add(ep);
            ep.ID = IDForPropType(cfp.PropertyType);
            
            // Get any cross-fill that's appropriate for this property
            if (CrossFillDescriptorRequested != null)
            {
                CrossFillDescriptorArgs args = new CrossFillDescriptorArgs(cfp.PropertyType);
                CrossFillDescriptorRequested(this, args);
                ep.CrossFillDescriptor = args.XFillDescriptor;
            }
            ep.Username = Username;
            ep.FlightProperty = cfp;
        }

        /// <summary>
        /// Recreates the combobox and propedit controls so that they exist for postback events.
        /// </summary>
        protected void RecreateControls()
        {
            if (plcHolderProps.Controls.Count > 0)  // don't do this if we've already set them up.
                return;

            List<CustomPropertyType> lstAll = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes());
            foreach (int idPropType in ActivePropTypes)
            {
                CustomPropertyType cpt = lstAll.Find(c => c.PropTypeID == idPropType);
                CustomFlightProperty cfp = new CustomFlightProperty(cpt);
                if (ExistingPropIDs.ContainsKey(idPropType))
                    cfp.PropID = ExistingPropIDs[idPropType];
                InsertEditProp(cfp);
                lstAll.Remove(cpt); // since it's here, make sure it isn't in the list of available types; we'll bind this below.

            }

            // recreate combo box - huge viewstate, so we just recreate it each time.
            // This loses the selected value, so we have to grab that directly from the form.
            cmbPropsToAdd.DataSource = lstAll;
            cmbPropsToAdd.DataBind();
            if (IsPostBack)
                cmbPropsToAdd.SelectedValue = Request.Form[cmbPropsToAdd.UniqueID];
        }

        protected void PopulateControls()
        {
            plcHolderProps.Controls.Clear();
            foreach (CustomFlightProperty fp in ActiveProperties)
                InsertEditProp(fp);
        }

        protected void RefreshList(CustomFlightProperty cfp = null, bool fStripDefaults = false)
        {
            Properties = PropertiesFromPropSet;             // Pick up any changes from the existing child controls, to preserve across postback
            if (cfp != null)
                Properties.Add(cfp);
            SegregateProperties(fStripDefaults);            // add the new property to the list
            PopulateControls();                             // And re-populate.
            txtFilter.Text = string.Empty;
            mfbSelectTemplates.Refresh();
        }

        /// <summary>
        /// Refreshes the propset, stripping default values.
        /// </summary>
        public void Refresh()
        {
            RefreshList(null, true);
        }

        protected void cmbPropsToAdd_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(cmbPropsToAdd.SelectedValue))
                return;
            int idPropType = Convert.ToInt32(cmbPropsToAdd.SelectedValue, CultureInfo.InvariantCulture);
            CustomPropertyType[] rgCptAll = CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.IsAuthenticated ? Username : string.Empty);
            CustomPropertyType cpt = rgCptAll.First((cpt2) => { return cpt2.PropTypeID == idPropType; });

            if (cpt == null)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Custom property type with id {0} not found!", idPropType));

            RefreshList(new CustomFlightProperty(cpt));
        }

        protected void mfbSelectTemplates_TemplateSelected(object sender, PropertyTemplateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Template == null)
                throw new ArgumentException("Null Template in PropertyTemplateEventArgs");

            AddTemplate(e.Template);
            Refresh();
        }

        protected void mfbSelectTemplates_TemplateUnselected(object sender, PropertyTemplateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            RemoveTemplate(e.TemplateID);
            Refresh();
        }

        protected void mfbSelectTemplates_TemplatesReady(object sender, EventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            // Hide the pop menu if only automatic templates are available
            if (mfbSelectTemplates.GroupedTemplates.Count() == 1 && mfbSelectTemplates.GroupedTemplates.ElementAt(0).Group == PropertyTemplateGroup.Automatic)
                popmenu.Visible = false;
        }

    }
}