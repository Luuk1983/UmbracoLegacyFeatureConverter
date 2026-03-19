using AutoBlockList.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;

namespace AutoBlockList.Services;

/// <summary>
/// Service for discovering and managing property converters.
/// Database-agnostic service that works with all Umbraco-supported databases.
/// </summary>
public class ConverterService(
    IEnumerable<Converters.IPropertyConverter> converters,
    IContentTypeService contentTypeService,
    ILogger<ConverterService> logger) : IConverterService
{
    public IEnumerable<Converters.IPropertyConverter> GetAllConverters()
    {
        return converters;
    }

            public Converters.IPropertyConverter? GetConverterByName(string converterName)
            {
                return converters.FirstOrDefault(c => 
                    c.ConverterName.Equals(converterName, StringComparison.OrdinalIgnoreCase));
            }

            public async Task<IEnumerable<ConverterMetadata>> GetConverterMetadataAsync()
            {
                var metadata = new List<ConverterMetadata>();

                foreach (var converter in converters)
                {
                    try
                    {
                        var count = await converter.GetAffectedDocumentTypesCountAsync();

                        metadata.Add(new ConverterMetadata
                        {
                            Name = converter.ConverterName,
                            Description = converter.Description,
                            SourceAliases = converter.SourcePropertyEditorAliases,
                            TargetAlias = converter.TargetPropertyEditorAlias,
                            AffectedDocumentTypesCount = count
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error getting metadata for converter {ConverterName}", converter.ConverterName);
                    }
                }

                return metadata;
            }

            public async Task<IEnumerable<DocumentTypeInfo>> GetAffectedDocumentTypesAsync(
                string converterName, 
                Guid[]? selectedKeys = null)
            {
                var converter = GetConverterByName(converterName);
                if (converter == null)
                {
                    logger.LogWarning("Converter {ConverterName} not found", converterName);
                    return Enumerable.Empty<DocumentTypeInfo>();
                }

                var allDocumentTypes = contentTypeService.GetAll().ToList();
                var documentTypeInfos = new List<DocumentTypeInfo>();

                // Filter by selection if provided
                if (selectedKeys != null && selectedKeys.Length > 0)
                {
                    allDocumentTypes = allDocumentTypes
                        .Where(dt => selectedKeys.Contains(dt.Key))
                        .ToList();
                }

                // Check each document type for properties using source property editors
                foreach (var docType in allDocumentTypes)
                {
                    bool hasSourceProperties = false;
                    int propertyCount = 0;

                    // Check direct properties
                    var directProps = docType.PropertyTypes
                        .Where(pt => converter.SourcePropertyEditorAliases.Contains(pt.PropertyEditorAlias))
                        .ToList();

                    hasSourceProperties = directProps.Any();
                    propertyCount += directProps.Count;

                    // Check composition properties
                    if (!hasSourceProperties && docType.ContentTypeComposition.Any())
                    {
                        var compositionProps = docType.CompositionPropertyTypes
                            .Where(pt => converter.SourcePropertyEditorAliases.Contains(pt.PropertyEditorAlias))
                            .ToList();

                        hasSourceProperties = compositionProps.Any();
                        propertyCount += compositionProps.Count;
                    }

                    if (hasSourceProperties)
                    {
                        documentTypeInfos.Add(new DocumentTypeInfo
                        {
                            Key = docType.Key,
                            Name = docType.Name,
                            Alias = docType.Alias,
                            Icon = docType.Icon,
                            PropertiesCount = propertyCount
                        });
                    }
                }

                return await Task.FromResult(documentTypeInfos.OrderBy(dt => dt.Name));
            }
        }
