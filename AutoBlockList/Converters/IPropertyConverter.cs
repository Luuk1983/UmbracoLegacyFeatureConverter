using System;
using System.Threading.Tasks;
using Umbraco.Community.LegacyFeatureConverter.Models;

namespace Umbraco.Community.LegacyFeatureConverter.Converters
{
    /// <summary>
    /// Defines the contract for property editor converters.
    /// Implementations convert legacy property editors to their modern equivalents.
    /// </summary>
    public interface IPropertyConverter
    {
        /// <summary>
        /// Gets the human-readable name of this converter.
        /// </summary>
        string ConverterName { get; }

        /// <summary>
        /// Gets the property editor aliases this converter can convert FROM.
        /// </summary>
        string[] SourcePropertyEditorAliases { get; }

        /// <summary>
        /// Gets the property editor alias this converter converts TO.
        /// </summary>
        string TargetPropertyEditorAlias { get; }

        /// <summary>
        /// Gets a brief description of what this converter does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes the conversion process with the specified options.
        /// </summary>
        /// <param name="options">The conversion options including document type selection and test run flag.</param>
        /// <returns>A task representing the asynchronous operation, with the conversion result.</returns>
        Task<ConversionResult> ExecuteConversionAsync(ConversionOptions options);

        /// <summary>
        /// Gets a count of document types that would be affected by this converter.
        /// </summary>
        /// <param name="selectedDocumentTypeKeys">Optional array of document type keys to filter. If null, counts all affected document types.</param>
        /// <returns>A task representing the asynchronous operation, with the count of affected document types.</returns>
        Task<int> GetAffectedDocumentTypesCountAsync(Guid[]? selectedDocumentTypeKeys = null);
    }
}
