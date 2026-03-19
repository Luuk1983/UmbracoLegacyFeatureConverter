using AutoBlockList.Data;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace AutoBlockList.Migrations;

/// <summary>
/// Notification handler that ensures the database schema is created on application startup.
/// Uses EF Core's schema creation based on OnModelCreating configuration.
/// Works with any database provider (SQL Server, SQLite, LocalDB, etc.).
/// </summary>
public class LegacyFeatureConverterDatabaseMigration(
    LegacyFeatureConverterDbContext dbContext,
    ILogger<LegacyFeatureConverterDatabaseMigration> logger) 
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Ensuring Legacy Feature Converter database schema exists");

            // EnsureCreated will create the database schema if it doesn't exist
            // It uses the configuration from OnModelCreating and generates the correct SQL
            // for whatever database provider is configured (SQL Server, SQLite, etc.)
            var created = await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            if (created)
            {
                logger.LogInformation("Successfully created Legacy Feature Converter database schema");
            }
            else
            {
                logger.LogDebug("Legacy Feature Converter database schema already exists");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure Legacy Feature Converter database schema");
            throw;
        }
    }
}

