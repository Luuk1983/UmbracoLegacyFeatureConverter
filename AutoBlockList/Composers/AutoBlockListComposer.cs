using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Community.LegacyFeatureConverter.Services.interfaces;
using Umbraco.Community.LegacyFeatureConverter.Backoffice;
using Umbraco.Community.LegacyFeatureConverter.Notifications;
using Umbraco.Community.LegacyFeatureConverter.Hubs;
using Umbraco.Community.LegacyFeatureConverter.Services;
using Umbraco.Community.LegacyFeatureConverter.Dtos;

namespace Umbraco.Community.LegacyFeatureConverter.Composers
{
    public class AutoBlockListComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<AutoBlockListManifestFilter>();

            builder.AddNotificationHandler<ContentTypeChangedNotification, ContentTypeChangedClearCacheHandler>()
                    .AddNotificationHandler<ContentSavedNotification, ContentClearCacheHandler>()
                    .AddNotificationHandler<ContentDeletedNotification, ContentClearCacheHandler>();

			builder.Services.AddSingleton<IAutoBlockListHubClientFactory, AutoBlockListHubClientFactory>()
							.AddScoped<IAutoBlockListContext, AutoBlockListContext>();

			builder.Services.AddScoped<IAutoBlockListService, AutoBlockListService>()
                            .AddScoped<IAutoBlockListMacroService, AutoBlockListMacroService>();

            builder.Services.AddOptions<AutoBlockListSettings>()
                .Bind(builder.Config.GetSection(AutoBlockListSettings.AutoBlockList));
        }
    }
}
