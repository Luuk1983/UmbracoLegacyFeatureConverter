using Umbraco.Cms.Core.Manifest;

namespace Umbraco.Community.LegacyFeatureConverter.Backoffice;

public class AutoBlockListManifestFilter : IManifestFilter
{
    public void Filter(List<PackageManifest> manifests)
    {
        manifests.Add(new PackageManifest
        {
            PackageName = "AutoBlockList",
            Scripts =
            [
                "/App_Plugins/LegacyFeatureConverter/backoffice/autoBlockList/overview.controller.js",
                "/App_Plugins/LegacyFeatureConverter/components/overlays/converting.controller.js",
                "/App_Plugins/LegacyFeatureConverter/backoffice/legacyConverter/overview.controller.js",
                "/App_Plugins/LegacyFeatureConverter/backoffice/legacyConverter/history.controller.js",
                "/App_Plugins/LegacyFeatureConverter/backoffice/legacyConverter/details.controller.js"
            ],
            Stylesheets =
            [
                "/App_Plugins/LegacyFeatureConverter/autoBlockList.css",
                "/App_Plugins/LegacyFeatureConverter/backoffice/legacyConverter/legacyConverter.css"
            ]
        });
    }
}
