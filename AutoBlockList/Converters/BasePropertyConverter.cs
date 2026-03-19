using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoBlockList.Models;
using AutoBlockList.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;

namespace AutoBlockList.Converters
{
    /// <summary>
    /// Base class for property editor converters providing common conversion workflow logic.
    /// Derived classes implement specific conversion logic for their property editor types.
    /// </summary>
    public abstract class BasePropertyConverter : IPropertyConverter
    {
        protected readonly ILogger _logger;
        protected readonly IDataTypeService _dataTypeService;
        protected readonly IContentTypeService _contentTypeService;
        protected readonly IContentService _contentService;
        protected readonly IConversionHistoryService _historyService;
        protected readonly IScopeProvider _scopeProvider;

        protected BasePropertyConverter(
            ILogger logger,
            IDataTypeService dataTypeService,
            IContentTypeService contentTypeService,
            IContentService contentService,
            IConversionHistoryService historyService,
            IScopeProvider scopeProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
            _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        }

        public abstract string ConverterName { get; }
        public abstract string[] SourcePropertyEditorAliases { get; }
        public abstract string TargetPropertyEditorAlias { get; }
        public abstract string Description { get; }

        /// <summary>
        /// Executes the conversion process using a standard workflow.
        /// For test runs, wraps everything in a scope without calling Complete().
        /// </summary>
        public virtual async Task<ConversionResult> ExecuteConversionAsync(ConversionOptions options)
        {
            var result = new ConversionResult
            {
                ConversionId = Guid.NewGuid(),
                ConverterType = ConverterName,
                IsTestRun = options.IsTestRun,
                StartedAt = DateTime.UtcNow,
                Status = ConversionStatus.Running
            };

            try
            {
                // Start history tracking
                await _historyService.StartConversionAsync(
                    result.ConversionId,
                    ConverterName,
                    options.IsTestRun,
                    options.SelectedDocumentTypeKeys,
                    options.PerformingUserKey);

                // Create a scope - for test runs, we won't call Complete()
                // autoComplete defaults to false, so scope is only committed when Complete() is called
                using (var scope = _scopeProvider.CreateScope(autoComplete: false))
                {
                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                        "Conversion", $"Starting {ConverterName} conversion", null);

                    // Phase 1: Scan document types for properties using source property editors
                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                        "Conversion", "Phase 1: Scanning document types", null);

                    var documentTypes = await ScanDocumentTypesAsync(options.SelectedDocumentTypeKeys);
                    result.DocumentTypes.AddRange(documentTypes.Select(dt => new DocumentTypeConversionInfo
                    {
                        Key = dt.Key,
                        Name = dt.Name,
                        Alias = dt.Alias
                    }));

                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                        "Conversion", $"Found {documentTypes.Count} document types to process", null);

                    // Phase 2: Create or get target data types
                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                        "Conversion", "Phase 2: Creating target data types", null);

                    var dataTypeMap = await CreateOrGetTargetDataTypesAsync(result, documentTypes);

                    // Phase 3: Update property types in document types (and compositions)
                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                        "Conversion", "Phase 3: Updating document type properties", null);

                    await UpdateDocumentTypePropertiesAsync(result, documentTypes, dataTypeMap);

                    // Phase 4: Convert content node data
                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                        "Conversion", "Phase 4: Converting content node data", null);

                    await ConvertContentDataAsync(result, documentTypes);

                    // Determine final status
                    result.Status = result.FailureCount > 0
                        ? (result.SuccessCount > 0 ? ConversionStatus.CompletedWithErrors : ConversionStatus.Failed)
                        : ConversionStatus.Completed;

                    result.CompletedAt = DateTime.UtcNow;

                    // CRITICAL: Only complete the scope if NOT a test run
                    if (!options.IsTestRun)
                    {
                        scope.Complete();
                        await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                            "Conversion", "Changes committed to database", null);
                    }
                    else
                    {
                        await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                            "Conversion", "Test run - changes rolled back (not saved)", null);
                    }
                }

                await _historyService.CompleteConversionAsync(result.ConversionId, result);

                _logger.LogInformation("Conversion {ConversionId} completed with status {Status}",
                    result.ConversionId, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during conversion {ConversionId}", result.ConversionId);

                result.Status = ConversionStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;
                result.CompletedAt = DateTime.UtcNow;

                await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Error,
                    "Conversion", $"Critical error: {ex.Message}", ex.StackTrace);

                await _historyService.CompleteConversionAsync(result.ConversionId, result);
            }

            return result;
        }

        public virtual async Task<int> GetAffectedDocumentTypesCountAsync(Guid[]? selectedDocumentTypeKeys = null)
        {
            var documentTypes = await ScanDocumentTypesAsync(selectedDocumentTypeKeys);
            return documentTypes.Count;
        }

        /// <summary>
        /// Scans all document types (or a filtered set) for properties using the source property editors.
        /// Includes checking compositions.
        /// </summary>
        protected virtual async Task<List<IContentType>> ScanDocumentTypesAsync(Guid[]? selectedDocumentTypeKeys)
        {
            var allDocumentTypes = _contentTypeService.GetAll().ToList();
            var affectedDocumentTypes = new List<IContentType>();

            // Filter by selection if provided
            if (selectedDocumentTypeKeys != null && selectedDocumentTypeKeys.Length > 0)
            {
                allDocumentTypes = allDocumentTypes
                    .Where(dt => selectedDocumentTypeKeys.Contains(dt.Key))
                    .ToList();
            }

            // Check each document type for properties using source property editors
            foreach (var docType in allDocumentTypes)
            {
                bool hasSourceProperties = false;

                // Check direct properties
                hasSourceProperties = docType.PropertyTypes.Any(pt =>
                    SourcePropertyEditorAliases.Contains(pt.PropertyEditorAlias));

                // Check composition properties if not already found
                if (!hasSourceProperties && docType.ContentTypeComposition.Any())
                {
                    hasSourceProperties = docType.CompositionPropertyTypes.Any(pt =>
                        SourcePropertyEditorAliases.Contains(pt.PropertyEditorAlias));
                }

                if (hasSourceProperties)
                {
                    affectedDocumentTypes.Add(docType);
                }
            }

            return await Task.FromResult(affectedDocumentTypes);
        }

        /// <summary>
        /// Creates or retrieves target data types for all source data types found.
        /// Returns a mapping of source data type ID to target data type.
        /// </summary>
        protected virtual async Task<Dictionary<int, IDataType>> CreateOrGetTargetDataTypesAsync(
            ConversionResult result,
            List<IContentType> documentTypes)
        {
            var dataTypeMap = new Dictionary<int, IDataType>();
            var processedDataTypeIds = new HashSet<int>();

            foreach (var docType in documentTypes)
            {
                // Get all properties from this document type (including compositions)
                var allProperties = docType.PropertyTypes
                    .Concat(docType.CompositionPropertyTypes)
                    .Where(pt => SourcePropertyEditorAliases.Contains(pt.PropertyEditorAlias))
                    .ToList();

                foreach (var property in allProperties)
                {
                    if (processedDataTypeIds.Contains(property.DataTypeId))
                        continue;

                    processedDataTypeIds.Add(property.DataTypeId);

                    var conversionInfo = new DataTypeConversionInfo
                    {
                        Id = property.DataTypeId,
                        Name = property.Name
                    };

                    try
                    {
                        var sourceDataType = _dataTypeService.GetDataType(property.DataTypeId);
                        if (sourceDataType == null)
                        {
                            conversionInfo.ErrorMessage = "Source data type not found";
                            result.DataTypes.Add(conversionInfo);
                            continue;
                        }

                        conversionInfo.Name = sourceDataType.Name;
                        conversionInfo.Key = sourceDataType.Key;

                        // Call derived class to create target data type
                        var targetDataType = await CreateTargetDataTypeAsync(sourceDataType);

                        if (targetDataType != null)
                        {
                            // Check if target data type already exists
                            var existing = _dataTypeService.GetDataType(targetDataType.Name);
                            if (existing == null)
                            {
                                _dataTypeService.Save(targetDataType);
                                await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                                    "DataType", $"Created data type: {targetDataType.Name}", sourceDataType.Key.ToString());
                            }
                            else
                            {
                                targetDataType = existing;
                                conversionInfo.Skipped = true;
                                await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                                    "DataType", $"Data type already exists: {existing.Name}", existing.Key.ToString());
                            }

                            dataTypeMap[property.DataTypeId] = targetDataType;
                            conversionInfo.Success = true;
                            conversionInfo.Message = conversionInfo.Skipped ? "Already exists" : "Created successfully";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating data type for {PropertyName}", property.Name);
                        conversionInfo.ErrorMessage = ex.Message;

                        await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Error,
                            "DataType", $"Error creating data type for {property.Name}: {ex.Message}",
                            ex.StackTrace);
                    }

                    result.DataTypes.Add(conversionInfo);
                }
            }

            return dataTypeMap;
        }

        /// <summary>
        /// Updates property types in document types to use the target data types.
        /// Handles both direct properties and composition properties.
        /// </summary>
        protected virtual async Task UpdateDocumentTypePropertiesAsync(
            ConversionResult result,
            List<IContentType> documentTypes,
            Dictionary<int, IDataType> dataTypeMap)
        {
            foreach (var docType in documentTypes)
            {
                var dtInfo = result.DocumentTypes.First(x => x.Key == docType.Key);

                try
                {
                    bool wasModified = false;

                    // Handle composition properties first
                    if (docType.ContentTypeComposition.Any())
                    {
                        wasModified = await UpdateCompositionPropertiesAsync(result, docType, dataTypeMap);
                    }

                    // Handle direct properties
                    var directProperties = docType.PropertyTypes
                        .Where(pt => SourcePropertyEditorAliases.Contains(pt.PropertyEditorAlias))
                        .ToList();

                    foreach (var property in directProperties)
                    {
                        if (dataTypeMap.TryGetValue(property.DataTypeId, out var targetDataType))
                        {
                            // Option A: Update DataTypeId in-place
                            property.DataTypeId = targetDataType.Id;
                            property.DataTypeKey = targetDataType.Key;
                            wasModified = true;
                            dtInfo.PropertiesUpdated++;

                            await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                                "Property", $"Updated property '{property.Name}' in document type '{docType.Name}'",
                                docType.Key.ToString());
                        }
                    }

                    if (wasModified)
                    {
                        _contentTypeService.Save(docType);
                        dtInfo.Success = true;
                        dtInfo.Message = $"Updated {dtInfo.PropertiesUpdated} properties";

                        await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                            "DocumentType", $"Saved document type: {docType.Name}", docType.Key.ToString());
                    }
                    else
                    {
                        dtInfo.Skipped = true;
                        dtInfo.Message = "No properties to update";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating document type {DocTypeName}", docType.Name);
                    dtInfo.ErrorMessage = ex.Message;

                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Error,
                        "DocumentType", $"Error updating document type {docType.Name}: {ex.Message}",
                        ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Updates properties in composition document types.
        /// </summary>
        protected virtual async Task<bool> UpdateCompositionPropertiesAsync(
            ConversionResult result,
            IContentType docType,
            Dictionary<int, IDataType> dataTypeMap)
        {
            bool anyModified = false;

            var compositionIds = docType.ContentTypeComposition.Select(c => c.Id).ToList();

            foreach (var compositionId in compositionIds)
            {
                var composition = _contentTypeService.Get(compositionId);
                if (composition == null) continue;

                var compositionProperties = composition.PropertyTypes
                    .Where(pt => SourcePropertyEditorAliases.Contains(pt.PropertyEditorAlias))
                    .ToList();

                bool compositionModified = false;

                foreach (var property in compositionProperties)
                {
                    if (dataTypeMap.TryGetValue(property.DataTypeId, out var targetDataType))
                    {
                        property.DataTypeId = targetDataType.Id;
                        property.DataTypeKey = targetDataType.Key;
                        compositionModified = true;

                        await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                            "Property", $"Updated property '{property.Name}' in composition '{composition.Name}'",
                            composition.Key.ToString());
                    }
                }

                if (compositionModified)
                {
                    _contentTypeService.Save(composition);
                    anyModified = true;

                    await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                        "DocumentType", $"Saved composition document type: {composition.Name}",
                        composition.Key.ToString());
                }
            }

            return anyModified;
        }

        /// <summary>
        /// Converts content node property data from source format to target format.
        /// Processes all content nodes using the affected document types.
        /// </summary>
        protected virtual async Task ConvertContentDataAsync(
            ConversionResult result,
            List<IContentType> documentTypes)
        {
            foreach (var docType in documentTypes)
            {
                // Get all content nodes of this document type
                var contentNodes = _contentService.GetPagedOfType(docType.Id, 0, int.MaxValue, out long totalRecords, null);

                foreach (var content in contentNodes)
                {
                    var contentInfo = new ContentConversionInfo
                    {
                        Id = content.Id,
                        Key = content.Key,
                        Name = content.Name
                    };

                    try
                    {
                        bool wasModified = false;

                        // Get all properties that were converted
                        var properties = content.Properties
                            .Where(p => SourcePropertyEditorAliases.Contains(p.PropertyType.PropertyEditorAlias))
                            .ToList();

                        foreach (var property in properties)
                        {
                            var oldValue = property.GetValue();
                            if (oldValue == null) continue;

                            // Call derived class to convert the property value
                            var newValue = await ConvertPropertyValueAsync(oldValue, property);

                            if (newValue != null)
                            {
                                property.SetValue(newValue);
                                wasModified = true;
                                contentInfo.PropertiesConverted++;

                                await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Information,
                                    "Content", $"Converted property '{property.Alias}' on content '{content.Name}'",
                                    content.Key.ToString());
                            }
                        }

                        if (wasModified)
                        {
                            _contentService.Save(content);
                            contentInfo.Success = true;
                            contentInfo.Message = $"Converted {contentInfo.PropertiesConverted} properties";
                        }
                        else
                        {
                            contentInfo.Skipped = true;
                            contentInfo.Message = "No properties to convert";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error converting content {ContentName} (ID: {ContentId})",
                            content.Name, content.Id);
                        contentInfo.ErrorMessage = ex.Message;

                        await _historyService.LogEntryAsync(result.ConversionId, LogLevel.Error,
                            "Content", $"Error converting content {content.Name}: {ex.Message}",
                            ex.StackTrace);
                    }

                    result.ContentNodes.Add(contentInfo);
                }
            }
        }

        /// <summary>
        /// Creates a target data type based on the source data type.
        /// Must be implemented by derived classes.
        /// </summary>
        protected abstract Task<IDataType?> CreateTargetDataTypeAsync(IDataType sourceDataType);

        /// <summary>
        /// Converts a property value from source format to target format.
        /// Must be implemented by derived classes.
        /// </summary>
        protected abstract Task<object?> ConvertPropertyValueAsync(object sourceValue, IProperty property);
    }
}
