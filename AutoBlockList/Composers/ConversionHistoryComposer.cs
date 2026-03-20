using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.LegacyFeatureConverter.Data;
using Umbraco.Community.LegacyFeatureConverter.Migrations;
using Umbraco.Community.LegacyFeatureConverter.Services;
using Umbraco.Community.LegacyFeatureConverter.Services.interfaces;
using Umbraco.Extensions;

namespace Umbraco.Community.LegacyFeatureConverter.Composers;

/// <summary>
/// Composer to register Entity Framework DbContext and history service.
/// </summary>
public class ConversionHistoryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Determine database provider at startup
        var connectionStrings = builder.Config.GetSection("ConnectionStrings").Get<ConnectionStrings>();
        var providerName = connectionStrings?.ProviderName ?? "Microsoft.Data.Sqlite";

        if (providerName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase))
        {
            // SQL Server - register SQL Server-specific context
            builder.Services.AddUmbracoDbContext<LegacyFeatureConverterDbContextSqlServer>(
                (serviceProvider, optionsBuilder) =>
                {
                    optionsBuilder.UseUmbracoDatabaseProvider(serviceProvider);
                });

            // ALSO register as base type so ConversionHistoryService can resolve it
            builder.Services.AddScoped<LegacyFeatureConverterDbContext>(sp => 
                sp.GetRequiredService<LegacyFeatureConverterDbContextSqlServer>());
        }
        else
        {
            // SQLite - register SQLite-specific context
            builder.Services.AddUmbracoDbContext<LegacyFeatureConverterDbContextSqlite>(
                (serviceProvider, optionsBuilder) =>
                {
                    optionsBuilder.UseUmbracoDatabaseProvider(serviceProvider);
                });

            // ALSO register as base type so ConversionHistoryService can resolve it
            builder.Services.AddScoped<LegacyFeatureConverterDbContext>(sp => 
                sp.GetRequiredService<LegacyFeatureConverterDbContextSqlite>());
        }

        // Register the history service
        builder.Services.AddScoped<IConversionHistoryService, ConversionHistoryService>();

        // Register migration runner to execute on application starting
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, LegacyFeatureConverterDatabaseMigration>();
    }
}
