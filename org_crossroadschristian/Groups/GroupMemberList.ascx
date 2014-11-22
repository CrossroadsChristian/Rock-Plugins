<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMemberList.ascx.cs" Inherits="RockWeb.Plugins.org_crossroadschristian.Groups.GroupMemberList" %>

<%@Import Namespace="Rock.Web.UI.Controls" %>
<asp:UpdatePanel ID="upList" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlContent" runat="server">

            <div id="pnlGroupMembers" runat="server">

                <div class="panel panel-block">
                
                    <div class="panel-heading clearfix">
                        <h1 class="panel-title pull-left">
                            <i class="fa fa-users"></i>
                            <asp:Literal ID="lHeading" runat="server" Text="Group Members" />
                        </h1>
                    </div>

                    <div class="panel-body">
                        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

                        <Rock:NotificationBox ID="nbRoleWarning" runat="server" NotificationBoxType="Warning" Title="No roles!" Visible="false" />

                        <div class="grid grid-panel">
                            <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue">
                                <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                                <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                                <Rock:RockCheckBoxList ID="cblRole" runat="server" Label="Role" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                                <Rock:RockCheckBoxList ID="cblStatus" runat="server" Label="Status" RepeatDirection="Horizontal" />
                            </Rock:GridFilter>
                            <Rock:Grid ID="gGroupMembers" runat="server" DisplayType="Full" AllowSorting="true" ><!--OnRowSelected="gGroupMembers_Edit"-->
                                <Columns>
                                    <Rock:SelectField />
                                    <asp:TemplateField>
                                        <HeaderTemplate>
                                            Photo
                                        </HeaderTemplate>
                                        <itemtemplate>
                                <a href='<%# FormatPersonLink(Eval("PersonId").ToString()) %>'>
                                    <img class="person-image" id="imgPersonImage" src="" runat="server"/>
                                </a>
                                </itemtemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="NickName" HeaderText="First Name" SortExpression="Person.NickName" />
                                    <asp:BoundField DataField="LastName" HeaderText="Last Name" SortExpression="Person.LastName" />
                                    <asp:BoundField DataField="GroupRole" HeaderText="Role" SortExpression="GroupRole.Name" />
                                    <!-- <asp:BoundField DataField="GroupMemberStatus" HeaderText="Status" SortExpression="GroupMemberStatus" /> -->
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </div>
                </div>
            </div>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript">

    var allCheckBoxSelector = '#<%=gGroupMembers.ClientID%> input[id*="cbAll"]:checkbox';
    var checkBoxSelector = '#<%=gGroupMembers.ClientID%> input[id*="cbSelected"]:checkbox';

    function ToggleCheckUncheckAllOptionAsNeeded() {
        var allCheckboxes = $(checkBoxSelector),
            checkedCheckboxes = allCheckboxes.filter(":checked"),
            noCheckboxesAreChecked = (checkedCheckboxes.length === 0),
            allCheckboxesAreChecked = (allCheckboxes.length === checkedCheckboxes.length);
        if (allCheckboxes.length === 0) {
            $(allCheckBoxSelector).hide();
        }
        else {
            $(allCheckBoxSelector).prop('checked', allCheckboxesAreChecked);
        }
    }

    function PerformCheck() {
        $("#<%=gGroupMembers.ClientID%>").on('click', 'input[id*="cbAll"]:checkbox', function () {
            $(checkBoxSelector).attr('checked', $(this).is(':checked'));
            $(checkBoxSelector).prop('checked', $(this).is(':checked'));
            ToggleCheckUncheckAllOptionAsNeeded();
        });

        $("#<%=gGroupMembers.ClientID%>").on('click', 'input[id*="cbSelected"]:checkbox', ToggleCheckUncheckAllOptionAsNeeded);
        ToggleCheckUncheckAllOptionAsNeeded();
    }

    //$(document).ready(function () {
    //    PerformCheck()
    //});

    Sys.Application.add_load(function () {
        PerformCheck();
        $('.grid-table i.flag').tooltip({ html: true, container: 'body', delay: { show: 250, hide: 100 } });

    });

</script>
