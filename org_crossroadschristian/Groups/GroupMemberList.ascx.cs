﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_crossroadschristian.Groups
{
    [DisplayName( "Group Member List" )]
    [Category( "Groups" )]
    [Description( "Lists all the members of the given group." )]

    [IntegerField("Photo Size", "The size of the preview photo. Default is 65.",false,65)]

    [GroupField( "Group", "Either pick a specific group or choose <none> to have group be determined by the groupId page parameter" )]
    [LinkedPage( "Detail Page" )]
    public partial class GroupMemberList : RockBlock, ISecondaryBlock
    {
        #region Private Variables

        private DefinedValueCache _inactiveStatus = null;
        private Group _group = null;
        BootstrapButton _bbtnTakeAttendance = new BootstrapButton();

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // if this block has a specific GroupId set, use that, otherwise, determine it from the PageParameters
            Guid groupGuid = GetAttributeValue( "Group" ).AsGuid();
            int groupId = 0;
            
            if ( groupGuid == Guid.Empty )
            {
                groupId = PageParameter( "GroupId" ).AsInteger();
            }

            if ( !(groupId == 0 && groupGuid == Guid.Empty ))
            {
                string key = string.Format( "Group:{0}", groupId );
                _group = RockPage.GetSharedItem( key ) as Group;
                if ( _group == null )
                {
                    _group = new GroupService( new RockContext() ).Queryable( "GroupType" )
                        .Where( g => g.Id == groupId || g.Guid == groupGuid )
                        .FirstOrDefault();
                    RockPage.SaveSharedItem( key, _group );
                }

                if ( _group != null )
                {
                    rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;
                    gGroupMembers.DataKeyNames = new string[] { "Id" };
                    gGroupMembers.CommunicateMergeFields = new List<string> { "GroupRole" };
                    gGroupMembers.PersonIdField = "PersonId";
                    gGroupMembers.RowDataBound += gGroupMembers_RowDataBound;
                    //gGroupMembers.Actions.AddClick += gGroupMembers_AddClick;
                    gGroupMembers.Actions.ShowAdd = false;
                    gGroupMembers.IsDeleteEnabled = false;
                    gGroupMembers.GridRebind += gGroupMembers_GridRebind;
                    gGroupMembers.RowItemText = _group.GroupType.GroupTerm + " " + _group.GroupType.GroupMemberTerm;
                    gGroupMembers.ExportFilename = _group.Name;

                    // make sure they have Auth to the block AND Edit to the Group
                    bool canEditBlock = IsUserAuthorized( Authorization.EDIT ) && _group.IsAuthorized( Authorization.EDIT, this.CurrentPerson );
                    gGroupMembers.Actions.ShowAdd = canEditBlock;
                    gGroupMembers.IsDeleteEnabled = canEditBlock;

                    // Add attribute columns
                    AddAttributeColumns();

                    // Add delete column
                    var deleteField = new DeleteField();
                    gGroupMembers.Columns.Add( deleteField );
                    deleteField.Click += DeleteGroupMember_Click;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindFilter();

                tbFirstName.Text = rFilter.GetUserPreference( "First Name" );
                tbLastName.Text = rFilter.GetUserPreference( "Last Name" );
                cblRole.SetValues( rFilter.GetUserPreference( "Role" ).Split( ';' ).ToList() );
                cblStatus.SetValues( rFilter.GetUserPreference( "Status" ).Split( ';' ).ToList() );

                BindGroupMembersGrid();
            }
        }

        #endregion

        #region GroupMembers Grid

        /// <summary>
        /// Handles the RowDataBound event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs"/> instance containing the event data.</param>
        void gGroupMembers_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var groupMember = e.Row.DataItem as GroupMember;
                if ( groupMember != null && groupMember.Person != null )
                {
                    if ( _inactiveStatus != null &&
                        groupMember.Person.RecordStatusValueId.HasValue &&
                        groupMember.Person.RecordStatusValueId == _inactiveStatus.Id )
                    {
                        e.Row.AddCssClass( "inactive" );
                    }

                    if ( groupMember.Person.IsDeceased ?? false )
                    {
                        e.Row.AddCssClass( "deceased" );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SaveUserPreference( "First Name", tbFirstName.Text );
            rFilter.SaveUserPreference( "Last Name", tbLastName.Text );
            rFilter.SaveUserPreference( "Role", GetCheckBoxListValues( cblRole ) );
            rFilter.SaveUserPreference( "Status", GetCheckBoxListValues( cblStatus ) );

            BindGroupMembersGrid();
        }

        /// <summary>
        /// Rs the filter_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "First Name":
                case "Last Name":
                    break;
                case "Role":
                    e.Value = ResolveValues( e.Value, cblRole );
                    break;
                case "Status":
                    e.Value = ResolveValues( e.Value, cblStatus );
                    break;
                default:
                    e.Value = string.Empty;
                    break;
            }
        }

        /// <summary>
        /// Handles the Click event of the DeleteGroupMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs" /> instance containing the event data.</param>
        protected void DeleteGroupMember_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            GroupMember groupMember = groupMemberService.Get( e.RowKeyId );
            if ( groupMember != null )
            {
                string errorMessage;
                if ( !groupMemberService.CanDelete( groupMember, out errorMessage ) )
                {
                    mdGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                int groupId = groupMember.GroupId;

                groupMemberService.Delete( groupMember );
                rockContext.SaveChanges();

                Group group = new GroupService( rockContext ).Get( groupId );
                if ( group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ) )
                {
                    // person removed from SecurityRole, Flush
                    Rock.Security.Role.Flush( group.Id );
                    Rock.Security.Authorization.Flush();
                }
            }

            BindGroupMembersGrid();
        }

        /// <summary>
        /// Handles the AddClick event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gGroupMembers_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "GroupMemberId", 0, "GroupId", _group.Id );
        }

        /// <summary>
        /// Handles the Edit event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gGroupMembers_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "GroupMemberId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gGroupMembers_GridRebind( object sender, EventArgs e )
        {
            BindGroupMembersGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            if ( _group != null )
            {
                cblRole.DataSource = _group.GroupType.Roles.OrderBy( a => a.Order ).ToList();
                cblRole.DataBind();
            }

            cblStatus.BindToEnum<GroupMemberStatus>();
        }

        /// <summary>
        /// Adds the attribute columns.
        /// </summary>
        private void AddAttributeColumns()
        {
            // Remove attribute columns
            foreach ( var column in gGroupMembers.Columns.OfType<AttributeField>().ToList() )
            {
                gGroupMembers.Columns.Remove( column );
            }

            if ( _group != null )
            {
                // Add attribute columns
                int entityTypeId = new GroupMember().TypeId;
                string groupQualifier = _group.Id.ToString();
                string groupTypeQualifier = _group.GroupTypeId.ToString();
                foreach ( var attribute in new AttributeService( new RockContext() ).Queryable()
                    .Where( a =>
                        a.EntityTypeId == entityTypeId &&
                        a.IsGridColumn &&
                        ( ( a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupQualifier ) ) ||
                         ( a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupTypeQualifier ) ) ) )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .ThenBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    string dataFieldExpression = attribute.Key;
                    bool columnExists = gGroupMembers.Columns.OfType<AttributeField>().FirstOrDefault( a => a.DataField.Equals( dataFieldExpression ) ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = dataFieldExpression;
                        boundField.HeaderText = attribute.Name;
                        boundField.SortExpression = string.Empty;

                        var attributeCache = Rock.Web.Cache.AttributeCache.Read( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gGroupMembers.Columns.Add( boundField );
                    }
                }
            }
        }

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void BindGroupMembersGrid()
        {
            if ( _group != null )
            {
                pnlGroupMembers.Visible = true;

                lHeading.Text = string.Format( "{0} {1}", _group.GroupType.GroupTerm, _group.GroupType.GroupMemberTerm.Pluralize() );

                if ( _group.GroupType.Roles.Any() )
                {
                    nbRoleWarning.Visible = false;
                    rFilter.Visible = true;
                    gGroupMembers.Visible = true;

                    GroupMemberService groupMemberService = new GroupMemberService( new RockContext() );
                    var qry = groupMemberService.Queryable( "Person,GroupRole", true )
                        .Where( m => m.GroupId == _group.Id );

                    // Filter by First Name
                    string firstName = rFilter.GetUserPreference( "First Name" );
                    if ( !string.IsNullOrWhiteSpace( firstName ) )
                    {
                        qry = qry.Where( m => m.Person.FirstName.StartsWith( firstName ) );
                    }

                    // Filter by Last Name
                    string lastName = rFilter.GetUserPreference( "Last Name" );
                    if ( !string.IsNullOrWhiteSpace( lastName ) )
                    {
                        qry = qry.Where( m => m.Person.LastName.StartsWith( lastName ) );
                    }

                    // Filter by role
                    var roles = new List<int>();
                    foreach ( string role in rFilter.GetUserPreference( "Role" ).Split( ';' ) )
                    {
                        if ( !string.IsNullOrWhiteSpace( role ) )
                        {
                            int roleId = int.MinValue;
                            if ( int.TryParse( role, out roleId ) )
                            {
                                roles.Add( roleId );
                            }
                        }
                    }

                    if ( roles.Any() )
                    {
                        qry = qry.Where( m => roles.Contains( m.GroupRoleId ) );
                    }

                    // Filter by Sttus
                    var statuses = new List<GroupMemberStatus>();
                    foreach ( string status in rFilter.GetUserPreference( "Status" ).Split( ';' ) )
                    {
                        if ( !string.IsNullOrWhiteSpace( status ) )
                        {
                            statuses.Add( status.ConvertToEnum<GroupMemberStatus>() );
                        }
                    }

                    if ( statuses.Any() )
                    {
                        qry = qry.Where( m => statuses.Contains( m.GroupMemberStatus ) );
                    }

                    _inactiveStatus = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );

                    SortProperty sortProperty = gGroupMembers.SortProperty;

                    List<GroupMember> groupMembers = null;

                    if ( sortProperty != null )
                    {
                        groupMembers = qry.Sort( sortProperty ).ToList();
                    }
                    else
                    {
                        groupMembers = qry.OrderBy( a => a.GroupRole.Order ).ThenBy( a => a.Person.LastName ).ThenBy( a => a.Person.FirstName ).ToList();
                    }

                    // Since we're not binding to actual group member list, but are using AttributeField columns,
                    // we need to save the workflows into the grid's object list
                    gGroupMembers.ObjectList = new Dictionary<string, object>();
                    groupMembers.ForEach( m => gGroupMembers.ObjectList.Add( m.Id.ToString(), m ) );

                    gGroupMembers.DataSource = groupMembers.Select( m => new
                    {
                        m.Id,
                        m.Guid,
                        m.PersonId,
                        NickName = m.Person.NickName,
                        LastName = m.Person.LastName,
                        GroupRole = m.GroupRole.Name,
                        m.GroupMemberStatus
                    } ).ToList();

                    gGroupMembers.DataBind();
                }
                else
                {
                    nbRoleWarning.Text = string.Format(
                        "{0} cannot be added to this {1} because the '{2}' group type does not have any roles defined.",
                        _group.GroupType.GroupMemberTerm.Pluralize(),
                        _group.GroupType.GroupTerm,
                        _group.GroupType.Name );

                    nbRoleWarning.Visible = true;
                    rFilter.Visible = false;
                    gGroupMembers.Visible = false;
                }
            }
            else
            {
                pnlGroupMembers.Visible = false;
            }
        }

        /// <summary>
        /// Gets the check box list values by evaluating the posted form values for each input item in the rendered checkbox list.  
        /// This is required because of a bug in ASP.NET that results in the Selected property for CheckBoxList items to not be
        /// set correctly on a postback.
        /// </summary>
        /// <param name="checkBoxList">The check box list.</param>
        /// <returns></returns>
        private string GetCheckBoxListValues( System.Web.UI.WebControls.CheckBoxList checkBoxList )
        {
            var selectedItems = new List<string>();

            for ( int i = 0; i < checkBoxList.Items.Count; i++ )
            {
                string value = Request.Form[checkBoxList.UniqueID + "$" + i.ToString()];
                if ( value != null )
                {
                    checkBoxList.Items[i].Selected = true;
                    selectedItems.Add( value );
                }
                else
                {
                    checkBoxList.Items[i].Selected = false;
                }
            }

            return selectedItems.AsDelimited( ";" );
        }

        /// <summary>
        /// Resolves the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="listControl">The list control.</param>
        /// <returns></returns>
        private string ResolveValues( string values, System.Web.UI.WebControls.CheckBoxList checkBoxList )
        {
            var resolvedValues = new List<string>();

            foreach ( string value in values.Split( ';' ) )
            {
                var item = checkBoxList.Items.FindByValue( value );
                if ( item != null )
                {
                    resolvedValues.Add( item.Text );
                }
            }

            return resolvedValues.AsDelimited( ", " );
        }

        /// <summary>
        /// Builds a link to the person's details page.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        protected string FormatPersonLink( string personId )
        {
            return ResolveRockUrl( string.Format( "~/Person/{0}", personId ) );
        }

        #endregion

        #region ISecondaryBlock

        /// <summary>
        /// Sets the visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlContent.Visible = visible;
        }

        #endregion
    }
}