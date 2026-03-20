using Microsoft.EntityFrameworkCore;

namespace Umbraco.Community.LegacyFeatureConverter.Data;

/// <summary>
/// SQL Server-specific DbContext for Legacy Feature Converter.
/// Used for generating SQL Server migrations and at runtime when SQL Server is configured.
/// </summary>
public class LegacyFeatureConverterDbContextSqlServer : LegacyFeatureConverterDbContext
{
    public LegacyFeatureConverterDbContextSqlServer(DbContextOptions<LegacyFeatureConverterDbContextSqlServer> options) 
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if not already configured (for design-time migrations)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TempMigrations;Trusted_Connection=True;");
        }
    }
}
