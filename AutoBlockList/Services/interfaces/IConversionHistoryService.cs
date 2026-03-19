using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoBlockList.Models;
using Microsoft.Extensions.Logging;

namespace AutoBlockList.Services.Interfaces
{
    /// <summary>
    /// Service for managing conversion history and logging.
    /// Stores conversion runs and detailed logs in the database for audit and debugging.
    /// </summary>
    public interface IConversionHistoryService
    {
        /// <summary>
        /// Starts a new conversion and creates a history record.
        /// </summary>
        /// <param name="conversionId">The unique identifier for this conversion run.</param>
        /// <param name="converterType">The type of converter being used.</param>
        /// <param name="isTestRun">Whether this is a test run (dry run).</param>
        /// <param name="selectedDocumentTypes">The document type keys that were selected, or null for all.</param>
        /// <param name="performingUserKey">The key of the user performing the conversion.</param>
        Task StartConversionAsync(
            Guid conversionId,
            string converterType,
            bool isTestRun,
            Guid[]? selectedDocumentTypes,
            Guid performingUserKey);

        /// <summary>
        /// Logs a single entry for the conversion.
        /// </summary>
        /// <param name="conversionId">The conversion ID this log belongs to.</param>
        /// <param name="level">The log level (Info, Warning, Error).</param>
        /// <param name="itemType">The type of item being logged (DocumentType, Content, Property, DataType).</param>
        /// <param name="message">The log message.</param>
        /// <param name="details">Optional additional details (will be stored as-is, or as JSON if object).</param>
        /// <param name="itemName">Optional name of the item being processed.</param>
        /// <param name="itemKey">Optional key/ID of the item being processed.</param>
        Task LogEntryAsync(
            Guid conversionId,
            LogLevel level,
            string itemType,
            string message,
            string? details,
            string? itemName = null,
            string? itemKey = null);

        /// <summary>
        /// Completes a conversion and updates the history record with final results.
        /// </summary>
        /// <param name="conversionId">The conversion ID to complete.</param>
        /// <param name="result">The final conversion result.</param>
        Task CompleteConversionAsync(Guid conversionId, ConversionResult result);

        /// <summary>
        /// Gets a specific conversion history record.
        /// </summary>
        /// <param name="conversionId">The conversion ID to retrieve.</param>
        /// <returns>The conversion history, or null if not found.</returns>
        Task<ConversionHistory?> GetHistoryAsync(Guid conversionId);

        /// <summary>
        /// Gets a paged list of conversion history records, ordered by date descending.
        /// </summary>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A list of conversion history records.</returns>
        Task<PagedResult<ConversionHistory>> GetHistoryListAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Gets all log entries for a specific conversion.
        /// </summary>
        /// <param name="conversionId">The conversion ID to get logs for.</param>
        /// <returns>A list of log entries ordered by timestamp.</returns>
        Task<IEnumerable<ConversionLogEntry>> GetLogEntriesAsync(Guid conversionId);
    }

    /// <summary>
    /// Represents a paged result set.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
