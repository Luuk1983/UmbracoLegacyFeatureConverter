using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoBlockList.Constants;
using AutoBlockList.Dtos;
using AutoBlockList.Dtos.BlockList;
using AutoBlockList.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants;
using static Umbraco.Cms.Core.PropertyEditors.BlockListConfiguration;
using DataType = Umbraco.Cms.Core.Models.DataType;

namespace AutoBlockList.Converters
{
    /// <summary>
    /// Converts Nested Content properties to Block List properties.
    /// Implements the document-type-first approach: scans all document types,
    /// creates data types, updates properties in-place, then converts content data.
    /// </summary>
    public class NestedContentConverter : BasePropertyConverter
    {
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IDataValueEditorFactory _dataValueEditorFactory;
        private readonly PropertyEditorCollection _propertyEditorCollection;
        private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;
        private readonly IOptions<AutoBlockListSettings> _settings;

        public NestedContentConverter(
            ILogger<NestedContentConverter> logger,
            IDataTypeService dataTypeService,
            IContentTypeService contentTypeService,
            IContentService contentService,
            IConversionHistoryService historyService,
            IScopeProvider scopeProvider,
            IShortStringHelper shortStringHelper,
            IDataValueEditorFactory dataValueEditorFactory,
            PropertyEditorCollection propertyEditorCollection,
            IConfigurationEditorJsonSerializer configurationEditorJsonSerializer,
            IOptions<AutoBlockListSettings> settings)
            : base(logger, dataTypeService, contentTypeService, contentService, historyService, scopeProvider)
        {
            _shortStringHelper = shortStringHelper ?? throw new ArgumentNullException(nameof(shortStringHelper));
            _dataValueEditorFactory = dataValueEditorFactory ?? throw new ArgumentNullException(nameof(dataValueEditorFactory));
            _propertyEditorCollection = propertyEditorCollection ?? throw new ArgumentNullException(nameof(propertyEditorCollection));
            _configurationEditorJsonSerializer = configurationEditorJsonSerializer ?? throw new ArgumentNullException(nameof(configurationEditorJsonSerializer));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public override string ConverterName => "Nested Content to Block List";

        public override string[] SourcePropertyEditorAliases => new[]
        {
            PropertyEditors.Aliases.NestedContent
        };

        public override string TargetPropertyEditorAlias => PropertyEditors.Aliases.BlockList;

        public override string Description => "Converts Nested Content properties to Block List properties, preserving all content and structure.";

        /// <summary>
        /// Creates a Block List data type based on the Nested Content configuration.
        /// Maps min/max items, content types, and block settings.
        /// </summary>
        protected override async Task<IDataType?> CreateTargetDataTypeAsync(IDataType sourceDataType)
        {
            var ncConfig = sourceDataType.Configuration as NestedContentConfiguration;
            if (ncConfig == null)
            {
                _logger.LogWarning("Source data type {DataTypeName} does not have NestedContentConfiguration", sourceDataType.Name);
                return null;
            }

            var blDataType = new DataType(new DataEditor(_dataValueEditorFactory), _configurationEditorJsonSerializer)
            {
                Editor = _propertyEditorCollection.First(x => x.Alias == PropertyEditors.Aliases.BlockList),
                CreateDate = DateTime.Now,
                Name = $"[Block List] {sourceDataType.Name}",
                Configuration = new BlockListConfiguration
                {
                    ValidationLimit = new BlockListConfiguration.NumberRange
                    {
                        Max = ncConfig.MaxItems,
                        Min = ncConfig.MinItems
                    },
                },
            };

            var blConfig = blDataType.Configuration as BlockListConfiguration;
            var blocks = new List<BlockConfiguration>();

            // Map each nested content type to a block configuration
            foreach (var ncContentType in ncConfig.ContentTypes)
            {
                var elementType = _contentTypeService.Get(ncContentType.Alias);
                if (elementType == null)
                {
                    _logger.LogWarning("Element type {Alias} not found for nested content item", ncContentType.Alias);
                    continue;
                }

                blocks.Add(new BlockConfiguration
                {
                    Label = ncContentType.Template,
                    EditorSize = _settings.Value.BlockListEditorSize,
                    ContentElementTypeKey = elementType.Key
                });
            }

            if (blConfig != null)
            {
                blConfig.Blocks = blocks.ToArray();
            }

            return await Task.FromResult(blDataType);
        }

        /// <summary>
        /// Converts a Nested Content property value to Block List format.
        /// Handles both culture-variant and invariant properties, including nested NC.
        /// </summary>
        protected override async Task<object?> ConvertPropertyValueAsync(object sourceValue, IProperty property)
        {
            if (sourceValue == null)
            {
                _logger.LogDebug("Property value is null for {PropertyAlias}", property.Alias);
                return null;
            }

            var valueString = sourceValue.ToString();
            if (string.IsNullOrEmpty(valueString))
            {
                _logger.LogDebug("Property value is empty for {PropertyAlias}", property.Alias);
                return null;
            }

            try
            {
                // Deserialize nested content array
                var jArray = JsonConvert.DeserializeObject<JArray>(valueString);
                if (jArray == null || jArray.Count == 0)
                {
                    _logger.LogDebug("No nested content items found for {PropertyAlias}", property.Alias);
                    return null;
                }

                _logger.LogInformation("Converting {Count} nested content items for property {PropertyAlias}", 
                    jArray.Count, property.Alias);

                // Convert JArray to dictionary format
                var ncValues = jArray.Select(item =>
                {
                    var dict = new Dictionary<string, string?>();
                    var jObject = (JObject)item;

                    foreach (var prop in jObject.Properties())
                    {
                        if (prop.Value.Type == JTokenType.Null)
                        {
                            dict[prop.Name] = null;
                            continue;
                        }

                        string propValue = prop.Value.Type == JTokenType.String
                            ? prop.Value.Value<string>() ?? string.Empty
                            : prop.Value.ToString(Formatting.None);

                        dict[prop.Name] = propValue;
                    }
                    return dict;
                }).ToList();

                // Convert nested content data to block list data
                var contentData = ConvertNCDataToBLData(ncValues);
                if (contentData == null || !contentData.Any())
                {
                    _logger.LogWarning("No content data after conversion for {PropertyAlias}", property.Alias);
                    return null;
                }

                // Build the layout (content UDI references)
                var contentUdiList = new List<Dictionary<string, string>>();
                foreach (var content in contentData)
                {
                    var udi = content["udi"];
                    if (udi != null)
                    {
                        contentUdiList.Add(new Dictionary<string, string>
                        {
                            {"contentUdi", udi },
                        });
                    }
                }

                // Create the block list structure
                var blockList = new BlockList
                {
                    layout = new BlockListUdi(contentUdiList, new List<Dictionary<string, string>>()),
                    contentData = contentData,
                    settingsData = new List<Dictionary<string, string?>>()
                };

                return JsonConvert.SerializeObject(blockList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert nested content for property {PropertyAlias}", property.Alias);
                return null;
            }
        }

        /// <summary>
        /// Recursively converts nested content data to block list data.
        /// Handles nested NC properties within NC items.
        /// </summary>
        private List<Dictionary<string, string?>>? ConvertNCDataToBLData(IEnumerable<Dictionary<string, string?>> ncValues)
        {
            if (ncValues == null || !ncValues.Any())
            {
                _logger.LogDebug("ConvertNCDataToBLData: No items to convert");
                return null;
            }

            var contentData = new List<Dictionary<string, string?>>();

            foreach (var ncValue in ncValues)
            {
                var rawContentType = ncValue.FirstOrDefault(x => x.Key == "ncContentTypeAlias").Value;
                if (string.IsNullOrEmpty(rawContentType))
                {
                    _logger.LogWarning("NC item has no ncContentTypeAlias, skipping");
                    continue;
                }

                var contentType = _contentTypeService.GetAllElementTypes().FirstOrDefault(x => x.Alias == rawContentType);
                if (contentType == null)
                {
                    _logger.LogWarning(
                        "Element type with alias '{Alias}' was not found. " +
                        "Verify the content type exists and is marked as an Element Type.",
                        rawContentType);
                    continue;
                }

                var contentUdi = new GuidUdi("element", Guid.NewGuid()).ToString();
                var values = ncValue.Where(x => !AutoBlockListConstants.DefaultNC.Contains(x.Key));

                var content = new Dictionary<string, string?>
                {
                    {"contentTypeKey", contentType.Key.ToString() },
                    {"udi", contentUdi },
                };

                // Process each property value
                foreach (var value in values)
                {
                    if (value.Value == null)
                    {
                        _logger.LogDebug("Property {PropName} has null value, skipping", value.Key);
                        continue;
                    }

                    try
                    {
                        // Check if this property contains nested NC
                        IEnumerable<Dictionary<string, string?>>? nestedNCValues = null;
                        try
                        {
                            var jArray = JsonConvert.DeserializeObject<JArray>(value.Value);
                            if (jArray != null)
                            {
                                nestedNCValues = jArray.Select(item =>
                                {
                                    var dict = new Dictionary<string, string?>();
                                    foreach (var prop in ((JObject)item).Properties())
                                    {
                                        if (prop.Value.Type == JTokenType.Null)
                                        {
                                            dict[prop.Name] = null;
                                            continue;
                                        }

                                        string nestedPropValue = prop.Value.Type == JTokenType.String
                                            ? prop.Value.Value<string>() ?? string.Empty
                                            : prop.Value.ToString(Formatting.None);

                                        dict[prop.Name] = nestedPropValue;
                                    }
                                    return dict;
                                }).ToList();
                            }
                        }
                        catch
                        {
                            // Not a valid JSON array, will be copied as-is
                        }

                        // Check if it's actually nested content (has ncContentTypeAlias)
                        if (nestedNCValues != null && nestedNCValues.Any(x => x.ContainsKey("ncContentTypeAlias")))
                        {
                            _logger.LogInformation("Property {PropName}: Detected as nested NC, converting recursively", value.Key);

                            var nestedContentData = ConvertNCDataToBLData(nestedNCValues);
                            if (nestedContentData == null || !nestedContentData.Any())
                            {
                                _logger.LogWarning("Skipping nested BL conversion for property '{Key}': conversion returned no items", value.Key);
                                continue;
                            }

                            var contentUdiList = new List<Dictionary<string, string>>();
                            foreach (var nestedContent in nestedContentData)
                            {
                                var nestedUdi = nestedContent["udi"];
                                if (nestedUdi != null)
                                {
                                    contentUdiList.Add(new Dictionary<string, string>
                                    {
                                        {"contentUdi", nestedUdi },
                                    });
                                }
                            }

                            var nestedBlockList = new BlockList
                            {
                                layout = new BlockListUdi(contentUdiList, new List<Dictionary<string, string>>()),
                                contentData = nestedContentData,
                                settingsData = new List<Dictionary<string, string?>>()
                            };

                            content[value.Key] = JsonConvert.SerializeObject(nestedBlockList);
                        }
                        else
                        {
                            // Not nested content, copy value as-is
                            content[value.Key] = value.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing property {PropName}, copying as-is", value.Key);
                        content[value.Key] = value.Value;
                    }
                }

                contentData.Add(content);
            }

            return contentData.Any() ? contentData : null;
        }
    }
}
