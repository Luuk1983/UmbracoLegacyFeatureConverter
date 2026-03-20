using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Community.LegacyFeatureConverter.Models;
using Umbraco.Community.LegacyFeatureConverter.Converters;

namespace Umbraco.Community.LegacyFeatureConverter.Services.interfaces
{
    /// <summary>
    /// Service for discovering and managing property converters.
    /// Provides access to all registered converters and their metadata.
    /// </summary>
    public interface IConverterService
    {
        /// <summary>
        /// Gets all registered property converters.
        /// </summary>
        IEnumerable<IPropertyConverter> GetAllConverters();

        /// <summary>
        /// Gets a specific converter by its name.
        /// </summary>
        IPropertyConverter? GetConverterByName(string converterName);

        /// <summary>
        /// Gets converter metadata for display in UI.
        /// </summary>
        Task<IEnumerable<ConverterMetadata>> GetConverterMetadataAsync();

        /// <summary>
        /// Gets document types that would be affected by a specific converter.
        /// </summary>
        Task<IEnumerable<DocumentTypeInfo>> GetAffectedDocumentTypesAsync(string converterName, Guid[]? selectedKeys = null);
    }

    /// <summary>
    /// Metadata about a converter for UI display.
    /// </summary>
    public class ConverterMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] SourceAliases { get; set; } = Array.Empty<string>();
        public string TargetAlias { get; set; } = string.Empty;
        public int AffectedDocumentTypesCount { get; set; }
    }

    /// <summary>
    /// Information about a document type for selection.
    /// </summary>
    public class DocumentTypeInfo
    {
        public Guid Key { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int PropertiesCount { get; set; }
    }
}
