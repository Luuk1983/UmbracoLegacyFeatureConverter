using AutoBlockList.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoBlockList.Data;

/// <summary>
/// DbContext for Legacy Feature Converter package.
/// Handles conversion history and log entries.
/// Schema is database-agnostic and works with SQL Server, SQLite, LocalDB, etc.
/// </summary>
public class LegacyFeatureConverterDbContext(DbContextOptions<LegacyFeatureConverterDbContext> options) 
    : DbContext(options)
{
    /// <summary>
    /// Gets or sets the conversion history records.
    /// </summary>
    public DbSet<ConversionHistory> ConversionHistories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the conversion log entries.
    /// </summary>
    public DbSet<ConversionLogEntry> ConversionLogs { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureConversionHistory(modelBuilder);
        ConfigureConversionLogEntry(modelBuilder);
    }

        /// <summary>
        /// Configures the ConversionHistory entity.
        /// </summary>
        private static void ConfigureConversionHistory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConversionHistory>(entity =>
            {
                entity.ToTable("LegacyFeatureConverterHistory");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .IsRequired();
                
                entity.Property(e => e.StartedAt)
                    .HasColumnName("StartedAt")
                    .IsRequired();
                
                entity.Property(e => e.CompletedAt)
                    .HasColumnName("CompletedAt");
                
                entity.Property(e => e.ConverterType)
                    .HasColumnName("ConverterType")
                    .HasMaxLength(200)
                    .IsRequired();
                
                entity.Property(e => e.IsTestRun)
                    .HasColumnName("IsTestRun")
                    .IsRequired();
                
                entity.Property(e => e.Status)
                    .HasColumnName("Status")
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(e => e.SelectedDocumentTypes)
                    .HasColumnName("SelectedDocumentTypes")
                    .HasColumnType("NTEXT");
                
                entity.Property(e => e.TotalDocumentTypes)
                    .HasColumnName("TotalDocumentTypes")
                    .HasDefaultValue(0)
                    .IsRequired();
                
                entity.Property(e => e.TotalDataTypes)
                    .HasColumnName("TotalDataTypes")
                    .HasDefaultValue(0)
                    .IsRequired();
                
                entity.Property(e => e.TotalContentNodes)
                    .HasColumnName("TotalContentNodes")
                    .HasDefaultValue(0)
                    .IsRequired();
                
                entity.Property(e => e.SuccessCount)
                    .HasColumnName("SuccessCount")
                    .HasDefaultValue(0)
                    .IsRequired();
                
                entity.Property(e => e.FailureCount)
                    .HasColumnName("FailureCount")
                    .HasDefaultValue(0)
                    .IsRequired();
                
                entity.Property(e => e.SkippedCount)
                    .HasColumnName("SkippedCount")
                    .HasDefaultValue(0)
                    .IsRequired();
                
                entity.Property(e => e.Summary)
                    .HasColumnName("Summary")
                    .HasColumnType("NTEXT");
                
                entity.Property(e => e.PerformingUserKey)
                    .HasColumnName("PerformingUserKey")
                    .IsRequired();

                // Configure one-to-many relationship with logs
                entity.HasMany(h => h.LogEntries)
                    .WithOne()
                    .HasForeignKey(l => l.ConversionHistoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Configures the ConversionLogEntry entity.
        /// </summary>
        private static void ConfigureConversionLogEntry(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConversionLogEntry>(entity =>
            {
                entity.ToTable("LegacyFeatureConverterLog");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .IsRequired();
                
                entity.Property(e => e.ConversionHistoryId)
                    .HasColumnName("ConversionHistoryId")
                    .IsRequired();
                
                entity.Property(e => e.Timestamp)
                    .HasColumnName("Timestamp")
                    .IsRequired();
                
                entity.Property(e => e.Level)
                    .HasColumnName("Level")
                    .HasMaxLength(20)
                    .IsRequired();
                
                entity.Property(e => e.ItemType)
                    .HasColumnName("ItemType")
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(e => e.ItemName)
                    .HasColumnName("ItemName")
                    .HasMaxLength(500);
                
                entity.Property(e => e.ItemKey)
                    .HasColumnName("ItemKey")
                    .HasMaxLength(200);
                
                entity.Property(e => e.Message)
                    .HasColumnName("Message")
                    .HasMaxLength(1000)
                    .IsRequired();
                
                entity.Property(e => e.Details)
                    .HasColumnName("Details")
                    .HasColumnType("NTEXT");
                
                entity.Property(e => e.StackTrace)
                    .HasColumnName("StackTrace")
                    .HasColumnType("NTEXT");

                // Index for faster queries
                entity.HasIndex(e => e.ConversionHistoryId)
                    .HasDatabaseName("IX_LegacyFeatureConverterLog_HistoryId");

                entity.HasIndex(e => e.Timestamp)
                    .HasDatabaseName("IX_LegacyFeatureConverterLog_Timestamp");
            });
        }
    }