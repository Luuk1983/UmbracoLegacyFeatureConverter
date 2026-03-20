using System;
using System.Collections.Generic;

namespace Umbraco.Community.LegacyFeatureConverter.Models
{
    /// <summary>
    /// Represents the complete result of a conversion operation.
    /// </summary>
    public class ConversionResult
    {
        /// <summary>
        /// Gets or sets the unique identifier for this conversion run.
        /// </summary>
        public Guid ConversionId { get; set; }

        /// <summary>
        /// Gets or sets the type of converter used.
        /// </summary>
        public string ConverterType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this was a test run.
        /// </summary>
        public bool IsTestRun { get; set; }

        /// <summary>
        /// Gets or sets the overall status of the conversion.
        /// </summary>
        public ConversionStatus Status { get; set; } = ConversionStatus.Running;

        /// <summary>
        /// Gets or sets the document types that were scanned.
        /// </summary>
        public List<DocumentTypeConversionInfo> DocumentTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the data types that were created or updated.
        /// </summary>
        public List<DataTypeConversionInfo> DataTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the content nodes that were converted.
        /// </summary>
        public List<ContentConversionInfo> ContentNodes { get; set; } = new();

        /// <summary>
        /// Gets the total count of items processed.
        /// </summary>
        public int TotalItems => DocumentTypes.Count + DataTypes.Count + ContentNodes.Count;

        /// <summary>
        /// Gets the count of successful operations.
        /// </summary>
        public int SuccessCount
        {
            get
            {
                int count = 0;
                count += DocumentTypes.FindAll(x => x.Success).Count;
                count += DataTypes.FindAll(x => x.Success).Count;
                count += ContentNodes.FindAll(x => x.Success).Count;
                return count;
            }
        }

        /// <summary>
        /// Gets the count of failed operations.
        /// </summary>
        public int FailureCount
        {
            get
            {
                int count = 0;
                count += DocumentTypes.FindAll(x => !x.Success && !x.Skipped).Count;
                count += DataTypes.FindAll(x => !x.Success && !x.Skipped).Count;
                count += ContentNodes.FindAll(x => !x.Success && !x.Skipped).Count;
                return count;
            }
        }

        /// <summary>
        /// Gets the count of skipped operations.
        /// </summary>
        public int SkippedCount
        {
            get
            {
                int count = 0;
                count += DocumentTypes.FindAll(x => x.Skipped).Count;
                count += DataTypes.FindAll(x => x.Skipped).Count;
                count += ContentNodes.FindAll(x => x.Skipped).Count;
                return count;
            }
        }

        /// <summary>
        /// Gets or sets when the conversion started.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets when the conversion completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets the duration of the conversion.
        /// </summary>
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

        /// <summary>
        /// Gets or sets any error message if the conversion failed critically.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception stack trace if available.
        /// </summary>
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// Status of a conversion operation.
    /// </summary>
    public enum ConversionStatus
    {
        /// <summary>
        /// Conversion is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Conversion completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Conversion completed with some errors.
        /// </summary>
        CompletedWithErrors,

        /// <summary>
        /// Conversion failed critically.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Information about a document type conversion.
    /// </summary>
    public class DocumentTypeConversionInfo
    {
        public Guid Key { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public int PropertiesUpdated { get; set; }
    }

    /// <summary>
    /// Information about a data type conversion.
    /// </summary>
    public class DataTypeConversionInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid Key { get; set; }
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Information about a content node conversion.
    /// </summary>
    public class ContentConversionInfo
    {
        public int Id { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public int PropertiesConverted { get; set; }
    }
}
