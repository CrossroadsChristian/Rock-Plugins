﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupTreeView.ascx.cs" Inherits="RockWeb.Plugins.org_crossroadschristian.Groups.GroupTreeView" %>

<asp:UpdatePanel ID="upGroupType" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="false">
    <ContentTemplate>
        <asp:HiddenField ID="hfRootGroupId" runat="server" ClientIDMode="Static" />
        <asp:HiddenField ID="hfInitialGroupId" runat="server" ClientIDMode="Static" />
        <asp:HiddenField ID="hfGroupTypes" runat="server" ClientIDMode="Static" />
        <asp:HiddenField ID="hfInitialGroupParentIds" runat="server" ClientIDMode="Static" />
        <asp:HiddenField ID="hfLimitToSecurityRoleGroups" runat="server" ClientIDMode="Static" />
        <asp:HiddenField ID="hfSelectedGroupId" runat="server" ClientIDMode="Static" />
        <asp:HiddenField ID="hfPageRouteTemplate" runat="server" ClientIDMode="Static" />

        <div class="treeview">
            <div class="treeview-actions" id="divTreeviewActions" runat="server">

                <div class="btn-group">
                    <button type="button" class="btn btn-action btn-xs dropdown-toggle" data-toggle="dropdown">
                        <i class="fa fa-plus-circle"></i>
                        <asp:Literal ID="ltAddCategory" runat="server" Text=" Add Group" />
                        <span class="fa fa-caret-down"></span>
                    </button>
                    <ul class="dropdown-menu" role="menu">
                        <li>
                            <asp:LinkButton ID="lbAddGroupRoot" OnClick="lbAddGroupRoot_Click" Text="Add Top-Level" runat="server"></asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="lbAddGroupChild" OnClick="lbAddGroupChild_Click" Enabled="false" Text="Add Child To Selected" runat="server"></asp:LinkButton></li>
                    </ul>
                </div>

            </div>

            <div class="treeview-scroll scroll-container scroll-container-horizontal">

                <div class="viewport">
                    <div class="overview">
                        <div class="panel-body treeview-frame">
                            <div id="treeview-content">
                            </div>
                        </div>

                    </div>
                </div>
                <div class="scrollbar">
                    <div class="track">
                        <div class="thumb">
                            <div class="end"></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>


        <script type="text/javascript">
            $(function () {
                var $selectedId = $('#hfSelectedGroupId'),
                    $expandedIds = $('#hfInitialGroupParentIds');

                var scrollbCategory = $('.treeview-scroll');
                scrollbCategory.tinyscrollbar({ axis: 'x', sizethumb: 60, size: 200 });

                // resize scrollbar when the window resizes
                $(document).ready(function () {
                    $(window).on('resize', function () {
                        resizeScrollbar(scrollbCategory);
                    });
                });

                $('#treeview-content')
                    .on('rockTree:selected', function (e, id) {
                        var groupSearch = '?GroupId=' + id;
                        var currentItemId = $selectedId.val();

                        if (currentItemId !== id) {

                            // get the data-id values of rock-tree items that are showing visible children (in other words, Expanded Nodes)
                            var expandedDataIds = $(e.currentTarget).find('.rocktree-children').filter(":visible").closest('.rocktree-item').map(function () {
                                return $(this).attr('data-id')
                            }).get().join(',');

                            var pageRouteTemplate = $('#hfPageRouteTemplate').val();
                            var locationUrl = "";
                            if (pageRouteTemplate.match(/{groupId}/i)) {
                                locationUrl = Rock.settings.get('baseUrl') + pageRouteTemplate.replace(/{groupId}/i, id);
                                locationUrl += "?ExpandedIds=" + encodeURIComponent(expandedDataIds);
                            }
                            else {
                                locationUrl = window.location.href.split('?')[0] + groupSearch;
                                locationUrl += "&ExpandedIds=" + encodeURIComponent(expandedDataIds);
                            }

                            window.location = locationUrl;
                        }
                    })
                    .on('rockTree:rendered', function () {

                        // update viewport height
                        resizeScrollbar(scrollbCategory);

                    })
                    .rockTree({
                        restUrl: '<%=ResolveUrl( "~/api/groups/getchildren/" ) %>',
                        restParams: '/' + ($('#hfRootGroupId').val() || 0) + '/' + ($('#hfLimitToSecurityRoleGroups').val() || false) + '/' + ($('#hfGroupTypes').val() || 0),
                        multiSelect: false,
                        selectedIds: $selectedId.val() ? $selectedId.val().split(',') : null,
                        expandedIds: $expandedIds.val() ? $expandedIds.val().split(',') : null
                    });
            });

            function resizeScrollbar(scrollControl) {
                var overviewHeight = $(scrollControl).find('.overview').height();

                $(scrollControl).find('.viewport').height(overviewHeight);

                scrollControl.tinyscrollbar_update('relative');
            }
        </script>

    </ContentTemplate>
</asp:UpdatePanel>
