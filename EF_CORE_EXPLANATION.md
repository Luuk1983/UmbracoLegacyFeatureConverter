# Entity Framework Core Implementation - Explanation

## Why OnModelCreating is Database-Agnostic

You're absolutely right to question the separate migrations per database type! Here's the explanation:

### The Umbraco Core Pattern (What I Initially Did - WRONG for a package)

**Umbraco CMS Core** uses separate migration projects:
- `Umbraco.Cms.Persistence.EFCore.SqlServer` - SQL Server migrations
- `Umbraco.Cms.Persistence.EFCore.Sqlite` - SQLite migrations

**Why Umbraco does this:**
1. They need EXACT control over every migration
2. They have thousands of tables and complex upgrade paths
3. They support upgrading from Umbraco 7, 8, 9, 10, 11, 12 → 13
4. They need to handle data transformations during upgrades

### The Package Pattern (What We Need - CORRECT)

**For a package**, we don't need separate migrations because:

1. **OnModelCreating is database-agnostic!**
   - You define the schema ONCE using Fluent API
   - EF Core generates the correct SQL for whatever provider is configured
   - SQL Server gets `NVARCHAR(200)`, SQLite gets `TEXT`, etc.

2. **EnsureCreated() handles everything:**
   ```csharp
   await dbContext.Database.EnsureCreatedAsync();
   ```
   - Checks if tables exist
   - If not, creates them using OnModelCreating configuration
   - Generates provider-specific SQL automatically
   - Works with ANY database EF Core supports

3. **No migration history tracking needed:**
   - We're creating new tables, not upgrading existing ones
   - Simple create-if-not-exists is sufficient
   - No complex data transformations

### How It Works

**OnModelCreating defines the schema:**
```csharp
entity.Property(e => e.ConverterType)
    .HasMaxLength(200)        // ← Database agnostic!
    .IsRequired();
```

**EF Core generates database-specific SQL:**

**SQL Server:**
```sql
CREATE TABLE [LegacyFeatureConverterHistory] (
    [ConverterType] NVARCHAR(200) NOT NULL
)
```

**SQLite:**
```sql
CREATE TABLE "LegacyFeatureConverterHistory" (
    "ConverterType" TEXT NOT NULL
)
```

**PostgreSQL (future):**
```sql
CREATE TABLE "LegacyFeatureConverterHistory" (
    "ConverterType" VARCHAR(200) NOT NULL
)
```

**All from the SAME OnModelCreating code!**

### What We Implemented

✅ **LegacyFeatureConverterDbContext** - Defines schema in OnModelCreating
✅ **Database.EnsureCreatedAsync()** - Creates tables on startup  
✅ **AddUmbracoDbContext<T>()** - Registers with Umbraco's DI
✅ **UseUmbracoDatabaseProvider()** - Detects SQL Server/SQLite at runtime
❌ **NO separate migration files needed!**

### Advantages

1. **Truly database-agnostic** - Works with any EF Core provider
2. **Simpler to maintain** - One schema definition
3. **Future-proof** - Easy to support PostgreSQL, MySQL, etc.
4. **No migration conflicts** - No separate files to keep in sync
5. **Package-friendly** - Minimal complexity

### When You WOULD Need Separate Migrations

Only if you need to:
- Upgrade from version 1.0 → 2.0 with schema changes
- Transform existing data during upgrade
- Support multiple versions simultaneously
- Match Umbraco's complex upgrade paths

For our use case (new tables, no upgrades yet), `OnModelCreating + EnsureCreated` is perfect!

---

## Modern C# Features Used

### File-Scoped Namespaces (C# 10+)
```csharp
// Old
namespace AutoBlockList.Services
{
    public class MyService { }
}

// New
namespace AutoBlockList.Services;

public class MyService { }
```

**Benefits:**
- One less indentation level
- Cleaner code
- Less visual clutter

### Primary Constructors (C# 12)
```csharp
// Old
public class MyService : IMyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

// New
public class MyService(ILogger<MyService> logger) : IMyService
{
    // logger is automatically available as a parameter
}
```

**Benefits:**
- Less boilerplate
- Automatic null checking
- Parameters available throughout class
- Cleaner, more concise

### Collection Expressions (C# 12)
```csharp
// Old
new List<ConversionHistory>()

// New
[]
```

---

## Final Implementation Summary

### What Changed from NPoco → EF Core:

**Removed:**
- ❌ NPoco `[Column]`, `[TableName]` annotations from models
- ❌ `IScopeProvider` and `scope.Database` in service
- ❌ Manual migration files (CreateConversionHistoryTablesMigration.cs)
- ❌ Separate SQL Server and SQLite migration files
- ❌ Design-time factory (not needed)

**Added:**
- ✅ NuGet packages: `Umbraco.Cms.Persistence.EFCore`, `Umbraco.Cms.Persistence.EFCore.SqlServer`
- ✅ `LegacyFeatureConverterDbContext` with `OnModelCreating`
- ✅ `ConversionHistory.LogEntries` navigation property
- ✅ EF Core-based `ConversionHistoryService`
- ✅ `LegacyFeatureConverterDatabaseMigration` with `EnsureCreatedAsync()`
- ✅ `AddUmbracoDbContext<T>()` registration in composer

**Modernized:**
- ✅ File-scoped namespaces in all new/updated files
- ✅ Primary constructors where applicable
- ✅ Collection expressions `[]` instead of `new List<>()`

### Database Support (All via EF Core):
✅ **SQL Server** - Full support, EF generates T-SQL
✅ **SQLite** - Full support, EF generates SQLite SQL  
✅ **SQL Server LocalDB** - Supported via SQL Server provider
✅ **Future databases** - Just install the EF provider!

### How Schema Creation Works:

**At Runtime:**
1. Umbraco starts up
2. `UmbracoApplicationStartedNotification` fires
3. `LegacyFeatureConverterDatabaseMigration` handles it
4. Calls `dbContext.Database.EnsureCreatedAsync()`
5. EF Core checks if tables exist
6. If not, generates SQL from `OnModelCreating`
7. Detects database provider (SQL Server/SQLite/etc.)
8. Generates provider-specific SQL
9. Executes CREATE TABLE statements
10. Tables ready to use!

**The beauty:** Same code, different databases! 🎉

