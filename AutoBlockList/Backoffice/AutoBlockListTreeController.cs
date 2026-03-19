using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.ModelBinders;
using static Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.LegacyFeatureConverter.Backoffice;

[PluginController("LegacyFeatureConverter")]
[Authorize(Policy = AuthorizationPolicies.TreeAccessDocumentTypes)]
[Tree("settings", "legacyConverter", SortOrder = 0, TreeTitle = "Legacy Feature Converter", TreeGroup = "settingsGroup")]
public class AutoBlockListTreeController(
    IMenuItemCollectionFactory menuItemCollectionFactory,
    ILocalizedTextService localizedTextService,
    UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
    IEventAggregator eventAggregator)
    : TreeController(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
{
    protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
    {
        var rootResult = base.CreateRootNode(queryStrings);
        if (rootResult.Result is not null)
            return rootResult;

        var root = rootResult.Value;

        root.RoutePath = string.Format("{0}/{1}/{2}", Applications.Settings, "legacyConverter", "overview");
        root.Icon = "icon-axis-rotation";
        root.HasChildren = false;
        root.MenuUrl = null;

        return root;
    }

    protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
    {
        var menu = menuItemCollectionFactory.Create();
        return menu;
    }

    protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
    {
        var nodes = new TreeNodeCollection();
        return nodes;
    }
}
