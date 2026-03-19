using System;

namespace AutoBlockList.Models
{
    /// <summary>
    /// Represents a single log entry for a conversion operation.
    /// Provides detailed information about each step of the conversion process.
    /// </summary>
    public class ConversionLogEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for this log entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the conversion history ID this log entry belongs to.
        /// </summary>
        public Guid ConversionHistoryId { get; set; }

        /// <summary>
        /// Gets or sets when this log entry was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the log level (Info, Warning, Error, Success).
        /// </summary>
        public string Level { get; set; } = "Info";

        /// <summary>
        /// Gets or sets the type of item being processed (DocumentType, Content, Property, DataType).
        /// </summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the item being processed.
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// Gets or sets the key or ID of the item being processed (for reference).
        /// </summary>
        public string? ItemKey { get; set; }

        /// <summary>
        /// Gets or sets the log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional details as JSON (optional).
        /// Can contain configuration, property values, etc.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the exception stack trace (only for errors).
        /// </summary>
        public string? StackTrace { get; set; }
    }
}
