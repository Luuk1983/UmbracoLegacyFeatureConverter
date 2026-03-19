using AutoBlockList.Data;
using AutoBlockList.Migrations;
using AutoBlockList.Services;
using AutoBlockList.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace AutoBlockList.Composers;

/// <summary>
/// Composer to register Entity Framework DbContext and history service.
/// </summary>
public class ConversionHistoryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register Entity Framework DbContext with Umbraco database provider
        builder.Services.AddUmbracoDbContext<LegacyFeatureConverterDbContext>(
            (serviceProvider, optionsBuilder) =>
            {
                optionsBuilder.UseUmbracoDatabaseProvider(serviceProvider);
            });

        // Register the history service
        builder.Services.AddScoped<IConversionHistoryService, ConversionHistoryService>();

        // Register migration runner to execute on startup
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, LegacyFeatureConverterDatabaseMigration>();
    }
}
