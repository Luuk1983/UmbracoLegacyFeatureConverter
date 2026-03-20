using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.LegacyFeatureConverter.Data;

namespace Umbraco.Community.LegacyFeatureConverter.Migrations;

/// <summary>
/// Handles the creation and migration of the Legacy Feature Converter database when the Umbraco application starts.
/// Uses provider-specific DbContext types to apply the correct migrations (SQL Server or SQLite).
/// </summary>
public class LegacyFeatureConverterDatabaseMigration(
    IServiceProvider serviceProvider,
    LegacyFeatureConverterDbContext dbContext,
    IOptionsMonitor<ConnectionStrings> connectionStrings,
    ILogger<LegacyFeatureConverterDatabaseMigration> logger) 
    : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        if (notification.RuntimeLevel != Umbraco.Cms.Core.RuntimeLevel.Run)
        {
            logger.LogInformation("LEGACY FEATURE CONVERTER: Skipping database migrations because runtime level is {RuntimeLevel}", 
                notification.RuntimeLevel);
            return;
        }

        var providerName = connectionStrings.CurrentValue.ProviderName;
        logger.LogInformation("LEGACY FEATURE CONVERTER: Running database migrations for provider: {ProviderName}", providerName);

        try
        {

            var hasMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            // Note: We registered the BASE context, but migrations are in derived contexts
            // EF Core will find the migrations through assembly scanning based on the provider configured
            await dbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("LEGACY FEATURE CONVERTER: Migrations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LEGACY FEATURE CONVERTER: An error occurred while migrating the database");
            throw;
        }
    }
}

