using System.Collections.Generic;

namespace Umbraco.Community.LegacyFeatureConverter.Models;

/// <summary>
/// Represents a conversion history record stored in the database.
/// Tracks each conversion run for audit and debugging purposes.
/// </summary>
public class ConversionHistory
{
        /// <summary>
        /// Gets or sets the unique identifier for this conversion run.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets when the conversion started.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets when the conversion completed (null if still running).
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the type of converter used (e.g., "NestedContent", "MediaPicker").
        /// </summary>
        public string ConverterType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this was a test run (dry run).
        /// </summary>
        public bool IsTestRun { get; set; }

        /// <summary>
        /// Gets or sets the status of the conversion.
        /// </summary>
        public string Status { get; set; } = "Running";

        /// <summary>
        /// Gets or sets the selected document type keys as a JSON array string.
        /// Null means all document types were processed.
        /// </summary>
        public string? SelectedDocumentTypes { get; set; }

        /// <summary>
        /// Gets or sets the total number of document types processed.
        /// </summary>
        public int TotalDocumentTypes { get; set; }

        /// <summary>
        /// Gets or sets the total number of data types created or updated.
        /// </summary>
        public int TotalDataTypes { get; set; }

        /// <summary>
        /// Gets or sets the total number of content nodes converted.
        /// </summary>
        public int TotalContentNodes { get; set; }

        /// <summary>
        /// Gets or sets the count of successful operations.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the count of failed operations.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Gets or sets the count of skipped operations.
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Gets or sets a JSON summary of the conversion result.
        /// Contains detailed breakdown of what was processed.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the key of the user who performed the conversion.
        /// </summary>
        public Guid PerformingUserKey { get; set; }

        /// <summary>
        /// Gets or sets the navigation property for related log entries.
        /// </summary>
        public ICollection<ConversionLogEntry> LogEntries { get; set; } = new List<ConversionLogEntry>();
    }
