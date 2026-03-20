using Microsoft.EntityFrameworkCore;

namespace Umbraco.Community.LegacyFeatureConverter.Data;

/// <summary>
/// SQLite-specific DbContext for Legacy Feature Converter.
/// Used for generating SQLite migrations and at runtime when SQLite is configured.
/// </summary>
public class LegacyFeatureConverterDbContextSqlite : LegacyFeatureConverterDbContext
{
    public LegacyFeatureConverterDbContextSqlite(DbContextOptions<LegacyFeatureConverterDbContextSqlite> options) 
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if not already configured (for design-time migrations)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=temp.db");
        }
    }
}
