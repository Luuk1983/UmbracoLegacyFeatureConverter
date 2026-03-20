using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Umbraco.Community.LegacyFeatureConverter.Data;

namespace Umbraco.Community.LegacyFeatureConverter.Migrations;

/// <summary>
/// Design-time factory for SQL Server migrations.
/// </summary>
public class LegacyFeatureConverterDbContextSqlServerFactory : IDesignTimeDbContextFactory<LegacyFeatureConverterDbContextSqlServer>
{
    public LegacyFeatureConverterDbContextSqlServer CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LegacyFeatureConverterDbContextSqlServer>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TempMigrations;Trusted_Connection=True;");
        return new LegacyFeatureConverterDbContextSqlServer(optionsBuilder.Options);
    }
}

/// <summary>
/// Design-time factory for SQLite migrations.
/// </summary>
public class LegacyFeatureConverterDbContextSqliteFactory : IDesignTimeDbContextFactory<LegacyFeatureConverterDbContextSqlite>
{
    public LegacyFeatureConverterDbContextSqlite CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LegacyFeatureConverterDbContextSqlite>();
        optionsBuilder.UseSqlite("Data Source=temp.db");
        return new LegacyFeatureConverterDbContextSqlite(optionsBuilder.Options);
    }
}
