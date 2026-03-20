using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Umbraco.Community.LegacyFeatureConverter.Data;
using Umbraco.Community.LegacyFeatureConverter.Models;
using Umbraco.Community.LegacyFeatureConverter.Services.interfaces;

namespace Umbraco.Community.LegacyFeatureConverter.Services;

/// <summary>
/// Service for managing conversion history using Entity Framework Core.
/// Provides methods to create, update, and query conversion records and logs.
/// Database-agnostic: works with SQL Server, SQLite, LocalDB, etc.
/// </summary>
public class ConversionHistoryService(
    LegacyFeatureConverterDbContext dbContext,
    ILogger<ConversionHistoryService> logger) : IConversionHistoryService
{
    public async Task StartConversionAsync(
        Guid conversionId,
        string converterType,
        bool isTestRun,
        Guid[]? selectedDocumentTypes,
        Guid performingUserKey)
    {
        try
        {
            var history = new ConversionHistory
            {
                Id = conversionId,
                StartedAt = DateTime.UtcNow,
                ConverterType = converterType,
                IsTestRun = isTestRun,
                Status = "Running",
                SelectedDocumentTypes = selectedDocumentTypes != null
                    ? JsonConvert.SerializeObject(selectedDocumentTypes)
                    : null,
                PerformingUserKey = performingUserKey
            };

            dbContext.ConversionHistories.Add(history);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Started conversion {ConversionId} ({ConverterType})", 
                conversionId, converterType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start conversion history");
            throw;
        }
    }

    public async Task LogEntryAsync(
        Guid conversionId,
        LogLevel level,
        string itemType,
        string message,
        string? details,
        string? itemName = null,
        string? itemKey = null)
    {
        try
        {
            var entry = new ConversionLogEntry
            {
                Id = Guid.NewGuid(),
                ConversionHistoryId = conversionId,
                Timestamp = DateTime.UtcNow,
                Level = level.ToString(),
                ItemType = itemType,
                ItemName = itemName,
                ItemKey = itemKey,
                Message = message,
                Details = details,
                StackTrace = level == LogLevel.Error ? details : null
            };

            dbContext.ConversionLogs.Add(entry);
            await dbContext.SaveChangesAsync();

            logger.Log(level, "{ItemType}: {Message}", itemType, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add log entry");
            throw;
        }
    }

    public async Task CompleteConversionAsync(Guid conversionId, ConversionResult result)
    {
        try
        {
            var history = await dbContext.ConversionHistories.FindAsync(conversionId);
            if (history == null)
            {
                logger.LogWarning("Conversion history {ConversionId} not found", conversionId);
                return;
            }

            // Update with final results
            history.CompletedAt = DateTime.UtcNow;
            history.Status = result.Status.ToString();
            history.TotalDocumentTypes = result.DocumentTypes.Count;
            history.TotalDataTypes = result.DataTypes.Count;
            history.TotalContentNodes = result.ContentNodes.Count;
            history.SuccessCount = result.SuccessCount;
            history.FailureCount = result.FailureCount;
            history.SkippedCount = result.SkippedCount;

            // Store summary as JSON
            var summary = new
            {
                result.Status,
                result.StartedAt,
                result.CompletedAt,
                result.Duration,
                DocumentTypes = result.DocumentTypes.Select(dt => new
                {
                    dt.Key,
                    dt.Name,
                    dt.Alias,
                    dt.Success,
                    dt.Skipped,
                    dt.PropertiesUpdated,
                    dt.ErrorMessage
                }),
                DataTypes = result.DataTypes.Select(dt => new
                {
                    dt.Id,
                    dt.Name,
                    dt.Key,
                    dt.Success,
                    dt.Skipped,
                    dt.ErrorMessage
                }),
                ContentNodes = result.ContentNodes.Select(cn => new
                {
                    cn.Id,
                    cn.Key,
                    cn.Name,
                    cn.Success,
                    cn.Skipped,
                    cn.PropertiesConverted,
                    cn.ErrorMessage
                })
            };

            history.Summary = JsonConvert.SerializeObject(summary);

            await dbContext.SaveChangesAsync();

            logger.LogInformation("Completed conversion {ConversionId} with status {Status}",
                conversionId, result.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete conversion history");
            throw;
        }
    }

    public async Task<ConversionHistory?> GetHistoryAsync(Guid conversionId)
    {
        try
        {
            return await dbContext.ConversionHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == conversionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get conversion history {ConversionId}", conversionId);
            return null;
        }
    }

    public async Task<IEnumerable<ConversionLogEntry>> GetLogEntriesAsync(Guid conversionId)
    {
        try
        {
            return await dbContext.ConversionLogs
                .AsNoTracking()
                .Where(l => l.ConversionHistoryId == conversionId)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get log entries for conversion {ConversionId}", conversionId);
            return Enumerable.Empty<ConversionLogEntry>();
        }
    }

    public async Task<PagedResult<ConversionHistory>> GetHistoryListAsync(int page, int pageSize)
    {
        try
        {
            var query = dbContext.ConversionHistories.AsNoTracking();

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(h => h.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ConversionHistory>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get conversion history list");

            return new PagedResult<ConversionHistory>
            {
                Items = [],
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = 0
            };
        }
    }
}
