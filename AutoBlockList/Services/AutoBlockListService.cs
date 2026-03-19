using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core;
using AutoBlockList.Dtos;
using Umbraco.Extensions;
using AutoBlockList.Constants;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Services;
using AutoBlockList.Dtos.BlockList;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.PropertyEditors;
using AutoBlockList.Services.interfaces;
using static Umbraco.Cms.Core.Constants;
using DataType = Umbraco.Cms.Core.Models.DataType;
using static Umbraco.Cms.Core.PropertyEditors.BlockListConfiguration;

namespace AutoBlockList.Services
{
    public class AutoBlockListService : IAutoBlockListService
    {
        private readonly ILogger<AutoBlockListService> _logger;
        private readonly IContentService _contentService;
        private readonly IAutoBlockListContext _hubContext;
        private readonly IDataTypeService _dataTypeService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataValueEditorFactory _dataValueEditorFactory;
        private readonly PropertyEditorCollection _propertyEditorCollection;
        private readonly IOptions<AutoBlockListSettings> _dataBlockConverterSettings;
        private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;

        public AutoBlockListService(ILogger<AutoBlockListService> logger,
            IContentService contentService,
            IAutoBlockListContext hubContext,
            IDataTypeService dataTypeService,
            IShortStringHelper shortStringHelper,
            IContentTypeService contentTypeService,
            IDataValueEditorFactory dataValueEditorFactory,
            PropertyEditorCollection propertyEditorCollection,
            IOptions<AutoBlockListSettings> dataBlockConverterSettings,
            IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        {
            _logger = logger;
            _contentService = contentService;
            _hubContext = hubContext;
            _dataTypeService = dataTypeService;
            _shortStringHelper = shortStringHelper;
            _contentTypeService = contentTypeService;
            _dataValueEditorFactory = dataValueEditorFactory;
            _propertyEditorCollection = propertyEditorCollection;
            _dataBlockConverterSettings = dataBlockConverterSettings;
            _configurationEditorJsonSerializer = configurationEditorJsonSerializer;
        }

        public string GetNameFormatting() => _dataBlockConverterSettings.Value.NameFormatting;
        public string GetAliasFormatting() => _dataBlockConverterSettings.Value.AliasFormatting;
        public bool GetSaveAndPublishSetting() => _dataBlockConverterSettings.Value.SaveAndPublish;
        public string GetBlockListEditorSize() => _dataBlockConverterSettings.Value.BlockListEditorSize;

        public IDataType? CreateBLDataType(IDataType ncDataType)
        {
            var ncConfig = ncDataType.Configuration as NestedContentConfiguration;

            var blDataType = new DataType(new DataEditor(_dataValueEditorFactory), _configurationEditorJsonSerializer)
            {
                Editor = _propertyEditorCollection.First(x => x.Alias == PropertyEditors.Aliases.BlockList),
                CreateDate = DateTime.Now,
                Name = string.Format(GetNameFormatting(), ncDataType.Name),
                Configuration = new BlockListConfiguration()
                {
                    ValidationLimit = new NumberRange()
                    {
                        Max = ncConfig?.MaxItems,
                        Min = ncConfig?.MinItems
                    },
                },
            };

            var blConfig = blDataType.Configuration as BlockListConfiguration;
            var blocks = new List<BlockConfiguration>();

            foreach (var ncContentType in ncConfig.ContentTypes)
            {
                blocks.Add(new BlockConfiguration()
                {
                    Label = ncContentType.Template,
                    EditorSize = GetBlockListEditorSize(),
                    ContentElementTypeKey = _contentTypeService.Get(ncContentType.Alias).Key
                });
            }

            blConfig.Blocks = blocks.ToArray();

            return blDataType;
        }

        public ConvertReport ConvertNCDataType(int id)
        {
            var convertReport = new ConvertReport()
            {
                Task = string.Format("Converting NC data type with id {0} to Block list", id),
            };

            try
            {
                IDataType dataType = _dataTypeService.GetDataType(id);
                convertReport.Task = string.Format("Converting '{0}' to Block list", dataType.Name);

                _hubContext.Client?.UpdateItem(convertReport.Task);

                var blDataType = CreateBLDataType(dataType);
                var existingDataType = _dataTypeService.GetDataType(blDataType.Name);

                if (blDataType.Name != existingDataType?.Name)
                {
                    _dataTypeService.Save(blDataType);

                    convertReport.Status = AutoBlockListConstants.Status.Success;

                    _hubContext.Client?.AddReport(convertReport);
                    return convertReport;
                }

                convertReport.Status = AutoBlockListConstants.Status.Skipped;

                _hubContext.Client?.AddReport(convertReport);

                return convertReport;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Failed to convert NC with id '{0}' to block list.", id));

                convertReport.ErrorMessage = AutoBlockListConstants.CheckLogs;
                convertReport.Status = AutoBlockListConstants.Status.Failed;

                return convertReport;
            }
        }

        public ConvertReport AddDataTypeToContentType(IContentType contentType, IDataType ncDataType)
        {
            var blDataType = _dataTypeService.GetDataType(string.Format(GetNameFormatting(), ncDataType.Name));
            var convertReport = new ConvertReport()
            {
                Task = string.Format("Adding data type '{0}' to document type '{1}'", blDataType.Name, contentType.Name),
                Status = AutoBlockListConstants.Status.Failed
            };

            _hubContext.Client?.UpdateItem(convertReport.Task);

            try
            {
                var propertyType = contentType.PropertyTypes.FirstOrDefault(x => x.DataTypeId == ncDataType.Id);
                var isComposition = contentType.CompositionIds().Any();

                propertyType = isComposition ? contentType.CompositionPropertyTypes.FirstOrDefault(x => x.DataTypeId == ncDataType.Id) : propertyType;

                if (propertyType == null)
                {
                    convertReport.ErrorMessage = "Property type not found for the specified data type";
                    _hubContext.Client?.AddReport(convertReport);
                    return convertReport;
                }

                if (contentType.PropertyTypeExists(string.Format(GetAliasFormatting(), propertyType.Alias)))
                {
                    convertReport.Status = AutoBlockListConstants.Status.Skipped;
                    _hubContext.Client?.AddReport(convertReport);
                    return convertReport;
                }

                if (isComposition)
                {
                    var compositionContentTypeIds = contentType.CompositionIds();
                    foreach (var compositionContentTypeId in compositionContentTypeIds)
                    {
                        var compositionContentType = _contentTypeService.Get(compositionContentTypeId);
                        if (compositionContentType != null && compositionContentType.PropertyTypeExists(propertyType.Alias))
                        {
                            if (compositionContentType.PropertyTypeExists(string.Format(GetAliasFormatting(), propertyType.Alias)))
                            {
                                convertReport.Status = AutoBlockListConstants.Status.Skipped;
                                _hubContext.Client?.AddReport(convertReport);
                                return convertReport;
                            }

                            compositionContentType.AddPropertyType(MapPropertyType(propertyType, ncDataType, blDataType),
                                    compositionContentType.PropertyGroups.FirstOrDefault(x => x.Id == propertyType.PropertyGroupId.Value).Alias);
                            _contentTypeService.Save(compositionContentType);
                            convertReport.Status = AutoBlockListConstants.Status.Success;
                        }
                    }
                }

                if (contentType.PropertyTypeExists(propertyType.Alias))
                {
                    contentType.AddPropertyType(MapPropertyType(propertyType, ncDataType, blDataType),
                                                contentType.PropertyGroups.FirstOrDefault(x => x.Id == propertyType.PropertyGroupId.Value).Alias);
                    _contentTypeService.Save(contentType);
                    convertReport.Status = AutoBlockListConstants.Status.Success;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add block list to document type");
                _hubContext.Client?.Done("failed");
                convertReport.ErrorMessage = AutoBlockListConstants.CheckLogs;
            }

            _hubContext.Client?.AddReport(convertReport);

            return convertReport;
        }

        public IEnumerable<CustomDisplayDataType> GetAllDataTypesWithAlias(string alias)
        {
            var dataTypes = new List<CustomDisplayDataType>();
            foreach (var dataType in _dataTypeService.GetAll().Where(x => x.EditorAlias == alias))
                dataTypes.Add(new CustomDisplayDataType()
                {
                    Id = dataType.Id,
                    Name = dataType.Name,
                    Icon = dataType.Editor?.Icon
                });

            return dataTypes;
        }

        public void TransferContent(int id)
        {
            var node = _contentService.GetById(id);
            if (node == null)
            {
                var convertReport = new ConvertReport()
                {
                    Task = "Coverting content",
                    ErrorMessage = string.Format("Failed to find node with id {0}", id),
                    Status = AutoBlockListConstants.Status.Failed
                };

                _hubContext.Client?.AddReport(convertReport);
            }

            var allNCProperties = node.Properties.Where(x => x.PropertyType.PropertyEditorAlias == PropertyEditors.Aliases.NestedContent);

            foreach (var ncProperty in allNCProperties)
            {
                if (ncProperty.PropertyType.VariesByCulture())
                {
                    foreach (var culture in node.AvailableCultures)
                    {
                        var report = new ConvertReport()
                        {
                            Task = string.Format("Converting '{0}' for culture '{1}' to block list content", ncProperty.PropertyType.Name, culture),
                            Status = AutoBlockListConstants.Status.Failed
                        };

                        _hubContext.Client?.UpdateItem(report.Task);

                        try
                        {
                            var value = ConvertPropertyValueToBlockList(ncProperty, culture);
                            if (!string.IsNullOrEmpty(value))
                            {
                                node.SetValue(string.Format(GetAliasFormatting(), ncProperty.Alias), value, culture);
                                report.Status = AutoBlockListConstants.Status.Success;
                            }
                            else
                            {
                                report.Status = AutoBlockListConstants.Status.Skipped;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to convert content '{0}' for culture '{1}' to block list", ncProperty.PropertyType.Name);
                            report.ErrorMessage = AutoBlockListConstants.CheckLogs;
                        }

                        _hubContext.Client?.AddReport(report);
                    }
                }
                else
                {
                    var report = new ConvertReport()
                    {
                        Task = string.Format("Converting '{0}' to block list content", ncProperty.PropertyType.Name),
                        Status = AutoBlockListConstants.Status.Failed
                    };

                    _hubContext.Client?.UpdateItem(report.Task);

                    try
                    {
                        var value = ConvertPropertyValueToBlockList(ncProperty);
                        if (!string.IsNullOrEmpty(value))
                        {
                            node.SetValue(string.Format(GetAliasFormatting(), ncProperty.Alias), value);
                            report.Status = AutoBlockListConstants.Status.Success;
                        }
                        else
                        {
                            report.Status = AutoBlockListConstants.Status.Skipped;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to convert content '{0}' to block list", ncProperty.PropertyType.Name);
                        report.ErrorMessage = AutoBlockListConstants.CheckLogs;
                    }

                    _hubContext.Client?.AddReport(report);
                }
            }

            if (GetSaveAndPublishSetting())
            {
                _contentService.SaveAndPublish(node);
            }
            else
            {
                _contentService.Save(node);
            }
        }

        public string ConvertPropertyValueToBlockList(IProperty property, string? culture = null)
        {
            var cultureName = culture ?? "invariant";
            _logger.LogInformation("Starting conversion for property '{PropertyAlias}' culture '{Culture}'", property.Alias, cultureName);

            var value = property.GetValue(culture);
            if (value == null)
            {
                _logger.LogInformation("Property value is null, returning empty string");
                return string.Empty;
            }

            _logger.LogDebug("Property value length: {Length} chars", value.ToString()?.Length ?? 0);

            // FIX 1: Wrap outer NC deserialization in try/catch.
            // Previously this had no error handling — a malformed outer NC JSON string
            // would propagate uncaught all the way to TransferContent and mark the
            // entire property conversion as Failed.
            //
            // FIX 3: Use JArray to deserialize, then convert to Dictionary<string, string?>.
            // This prevents issues with properties containing JSON-serialized strings
            // (like the Thuisarts custom property editor) where escape sequences in the
            // string value would cause deserialization errors when using Dictionary<string, string> directly.
            // Using string? allows us to preserve actual null values.
            IEnumerable<Dictionary<string, string?>> ncValues;
            try
            {
                _logger.LogDebug("Attempting JArray deserialization");
                var valueString = value?.ToString();
                if (string.IsNullOrEmpty(valueString))
                {
                    _logger.LogWarning("Property value toString is null or empty for property '{0}' culture '{1}'", property.Alias, culture);
                    return string.Empty;
                }

                var jArray = JsonConvert.DeserializeObject<JArray>(valueString);

                if (jArray == null)
                {
                    _logger.LogWarning("JArray deserialization returned null for property '{0}' culture '{1}'", property.Alias, culture);
                    return string.Empty;
                }

                _logger.LogInformation("Successfully deserialized JArray with {Count} items", jArray.Count);

                ncValues = jArray.Select(item => 
                {
                    var dict = new Dictionary<string, string?>();
                    var jObject = (JObject)item;

                    foreach (var prop in jObject.Properties())
                    {
                        // Store null values as actual null to preserve semantics
                        if (prop.Value.Type == JTokenType.Null)
                        {
                            _logger.LogDebug("Property '{PropName}' is null, storing as null", prop.Name);
                            dict[prop.Name] = null;
                            continue;
                        }

                        // Convert JToken to string, preserving JSON structure for complex values
                        string propValue;

                        if (prop.Value.Type == JTokenType.String)
                        {
                            propValue = prop.Value.Value<string>() ?? string.Empty;
                        }
                        else
                        {
                            propValue = prop.Value.ToString(Formatting.None);
                        }

                        dict[prop.Name] = propValue;

                        // Log properties that contain JSON structures
                        if (!string.IsNullOrEmpty(propValue) && (propValue.TrimStart().StartsWith("{") || propValue.TrimStart().StartsWith("[")))
                        {
                            _logger.LogDebug("Property '{PropName}' contains JSON structure (length: {Length})", prop.Name, propValue.Length);
                        }
                    }
                    return dict;
                }).ToList();

                if (ncValues == null || !ncValues.Any())
                {
                    _logger.LogWarning("No NC values found after conversion for property '{0}' culture '{1}'", property.Alias, culture);
                    return string.Empty;
                }

                _logger.LogInformation("Converted to {Count} NC value dictionaries", ncValues.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse NC JSON for property '{0}' culture '{1}'", property.Alias, culture);
                return string.Empty;
            }

            _logger.LogDebug("Starting ConvertNCDataToBLData");
            var contentData = ConvertNCDataToBLData(ncValues);

            if (contentData == null)
            {
                _logger.LogWarning("ConvertNCDataToBLData returned null for property '{0}' culture '{1}'", property.Alias, culture);
                return string.Empty;
            }

            if (!contentData.Any())
            {
                _logger.LogWarning("ConvertNCDataToBLData returned empty list for property '{0}' culture '{1}'", property.Alias, culture);
                return string.Empty;
            }

            _logger.LogInformation("Successfully converted {Count} content items", contentData.Count);

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

            var blockList = new BlockList()
            {
                layout = new BlockListUdi(contentUdiList, new List<Dictionary<string, string>>()),
                contentData = contentData,
                settingsData = new List<Dictionary<string, string>>()
            };

            return JsonConvert.SerializeObject(blockList);
        }

        private List<Dictionary<string, string?>>? ConvertNCDataToBLData(IEnumerable<Dictionary<string, string?>> ncValues)
        {
            if (ncValues == null)
            {
                _logger.LogDebug("ConvertNCDataToBLData: ncValues is null");
                return null;
            }

            _logger.LogDebug("ConvertNCDataToBLData: Processing {Count} NC items", ncValues.Count());
            var contentData = new List<Dictionary<string, string?>>();
            var itemIndex = 0;

            foreach (var ncValue in ncValues)
            {
                itemIndex++;
                _logger.LogDebug("Processing NC item {Index}", itemIndex);

                var rawContentType = ncValue.FirstOrDefault(x => x.Key == "ncContentTypeAlias").Value;

                if (string.IsNullOrEmpty(rawContentType))
                {
                    _logger.LogWarning("NC item {Index} has no ncContentTypeAlias, skipping", itemIndex);
                    continue;
                }

                _logger.LogDebug("NC item {Index} has content type alias: {Alias}", itemIndex, rawContentType);

                var contentType = _contentTypeService.GetAllElementTypes().FirstOrDefault(x => x.Alias == rawContentType);

                // FIX 2: Guard against null contentType.
                // Previously, if GetAllElementTypes() returned no match (wrong alias, type not
                // marked as element type, or a cache issue), contentType.Key on the next line
                // threw a NullReferenceException that was not caught here and propagated all
                // the way up to TransferContent, marking the whole conversion as Failed.
                if (contentType == null)
                {
                    _logger.LogWarning(
                        "Skipping NC element: element type with alias '{alias}' was not found in GetAllElementTypes(). " +
                        "Verify the content type exists and is marked as an Element Type.",
                        rawContentType);
                    continue;
                }

                _logger.LogDebug("Found element type: {Name} ({Key})", contentType.Name, contentType.Key);

                var contentUdi = new GuidUdi("element", Guid.NewGuid()).ToString();
                var values = ncValue.Where(x => !AutoBlockListConstants.DefaultNC.Contains(x.Key));

                _logger.LogDebug("NC item {Index} has {Count} non-default properties to process", itemIndex, values.Count());

                var content = new Dictionary<string, string?>
                {
                    {"contentTypeKey", contentType.Key.ToString() },
                    {"udi", contentUdi },
                };

                var propertyIndex = 0;
                foreach (var value in values)
                {
                    propertyIndex++;
                    _logger.LogDebug("Processing property {PropertyIndex}/{Total}: {PropName}", propertyIndex, values.Count(), value.Key);

                    // Skip entries where the value is null (e.g. "PropType": null in newer NC format).
                    // Dictionary<string, string> with Newtonsoft deserializes null JSON values as
                    // null strings — passing null to DeserializeObject below would throw.
                    if (value.Value == null)
                    {
                        _logger.LogDebug("Property {PropName} has null value, skipping", value.Key);
                        continue;
                    }

                    try
                    {
                        // FIX 3: Use JArray for nested NC deserialization (same as top-level fix).
                        // This prevents issues with nested properties containing JSON-serialized strings.
                        IEnumerable<Dictionary<string, string?>>? nestedNCValues = null;
                        try
                        {
                            _logger.LogDebug("Attempting nested JArray deserialization for property {PropName}", value.Key);
                            var jArray = JsonConvert.DeserializeObject<JArray>(value.Value);

                            if (jArray != null)
                            {
                                _logger.LogDebug("Property {PropName}: JArray with {Count} items", value.Key, jArray.Count);

                                nestedNCValues = jArray.Select(item =>
                                {
                                    var dict = new Dictionary<string, string?>();
                                    foreach (var prop in ((JObject)item).Properties())
                                    {
                                        // Store null values as actual null to preserve semantics
                                        if (prop.Value.Type == JTokenType.Null)
                                        {
                                            dict[prop.Name] = null;
                                            continue;
                                        }

                                        string nestedPropValue;

                                        if (prop.Value.Type == JTokenType.String)
                                        {
                                            nestedPropValue = prop.Value.Value<string>() ?? string.Empty;
                                        }
                                        else
                                        {
                                            nestedPropValue = prop.Value.ToString(Formatting.None);
                                        }

                                        dict[prop.Name] = nestedPropValue;
                                    }
                                    return dict;
                                }).ToList();
                            }
                            else
                            {
                                _logger.LogDebug("Property {PropName}: JArray deserialization returned null", value.Key);
                            }
                        }
                        catch
                        {
                            _logger.LogDebug("Property {PropName}: Not a valid JSON array, will copy as-is", value.Key);
                            // Not valid JSON array, will be handled in outer catch and copied as-is
                            throw;
                        }

                        // FIX 4: The definitive "is this actually Nested Content?" check.
                        // Previously the check was only `nestedNCValues != null`, which caused
                        // false positives for many non-NC property types that happen to contain
                        // valid JSON arrays:
                        //
                        //   "backgroundImage": "[]"
                        //       => deserializes to an empty IEnumerable (not null!)
                        //       => was incorrectly treated as nested NC
                        //       => built an empty BL and added it as "backgroundImageBL"
                        //       => silently produced wrong data in the output
                        //
                        //   "[{"key":"...","mediaKey":"..."}]"  (media picker)
                        //       => deserializes successfully (not null)
                        //       => was incorrectly treated as nested NC
                        //       => recursive call found no ncContentTypeAlias => NullRef on contentType.Key
                        //
                        // The definitive marker for Nested Content is the presence of
                        // "ncContentTypeAlias" in at least one element. No other Umbraco
                        // property editor produces arrays of objects with that key.
                        if (nestedNCValues != null && nestedNCValues.Any(x => x.ContainsKey("ncContentTypeAlias")))
                        {
                            _logger.LogInformation("Property {PropName}: Detected as nested NC, converting recursively", value.Key);

                            var nestedContentData = ConvertNCDataToBLData(nestedNCValues);
                            var contentUdiList = new List<Dictionary<string, string>>();

                            // FIX 5: Changed from `return null` to `continue`.
                            // Previously, if one nested element failed to convert, the entire
                            // parent conversion was aborted (return null propagated up and caused
                            // ConvertPropertyValueToBlockList to return string.Empty for the whole
                            // property). Now we skip only the single problematic nested element.
                            if (nestedContentData == null)
                            {
                                _logger.LogWarning(
                                    "Skipping nested BL conversion for property '{key}': ConvertNCDataToBLData returned null.",
                                    value.Key);
                                continue;
                            }

                            if (!nestedContentData.Any())
                            {
                                _logger.LogWarning(
                                    "Nested BL conversion for property '{key}' returned no items, skipping.",
                                    value.Key);
                                continue;
                            }

                            _logger.LogInformation("Successfully converted {Count} nested items for property {PropName}", nestedContentData.Count, value.Key);

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

                            var blockList = new BlockList()
                            {
                                layout = new BlockListUdi(contentUdiList, new List<Dictionary<string, string>>()),
                                contentData = nestedContentData,
                                settingsData = new List<Dictionary<string, string>>()
                            };

                            var blKey = string.Format(GetAliasFormatting(), value.Key);
                            content.Add(blKey, JsonConvert.SerializeObject(blockList));
                            _logger.LogDebug("Added nested BL to property {BLKey}", blKey);
                        }
                        else
                        {
                            _logger.LogDebug("Property {PropName}: Not NC (or empty/no ncContentTypeAlias), copying as-is", value.Key);
                            // Not NC: copy value as-is (covers plain strings, media pickers,
                            // empty arrays, numeric strings, RTE HTML, etc.)
                            if (!content.ContainsKey(value.Key))
                            {
                                content.Add(value.Key, value.Value);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Property {PropName}: Already exists in content dictionary. This indicates the NC data contains " +
                                    "pre-existing converted properties (e.g., properties ending in 'BL'). Skipping duplicate to avoid " +
                                    "data corruption. This is likely due to a previous partial migration attempt.",
                                    value.Key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Value could not be parsed as JSON at all (e.g. plain HTML string,
                        // plain text). Copy it through unchanged.
                        _logger.LogDebug(ex, "Property '{key}' value is not valid JSON, copying as-is.", value.Key);
                        if (!content.ContainsKey(value.Key))
                        {
                            content.Add(value.Key, value.Value);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Property {key}: Already exists in content dictionary while handling non-JSON value. " +
                                "This indicates corrupted NC data with duplicate properties. Skipping to prevent data loss.",
                                value.Key);
                        }
                    }
                }

                _logger.LogInformation("NC item {Index}: Successfully processed, adding to content data", itemIndex);
                contentData.Add(content);
            }

            _logger.LogInformation("ConvertNCDataToBLData: Returning {Count} content items", contentData.Count);
            return contentData;
        }

        public PropertyType? MapPropertyType(IPropertyType propertyType, IDataType ncDataType, IDataType blDataType)
        {
            return new PropertyType(_shortStringHelper, ncDataType)
            {
                DataTypeId = blDataType.Id,
                DataTypeKey = blDataType.Key,
                PropertyEditorAlias = blDataType.EditorAlias,
                ValueStorageType = ncDataType.DatabaseType,
                Name = propertyType.Name,
                Alias = string.Format(GetAliasFormatting(), propertyType.Alias),
                CreateDate = DateTime.Now,
                Description = propertyType.Description,
                Mandatory = propertyType.Mandatory,
                MandatoryMessage = propertyType.MandatoryMessage,
                ValidationRegExp = propertyType.ValidationRegExp,
                ValidationRegExpMessage = propertyType.ValidationRegExpMessage,
                Variations = propertyType.Variations,
                LabelOnTop = propertyType.LabelOnTop,
                PropertyGroupId = propertyType.PropertyGroupId,
                SupportsPublishing = propertyType.SupportsPublishing,
                SortOrder = propertyType.SortOrder,
            };
        }

        public IEnumerable<IDataType> GetDataTypesInContentType(IContentType contentType)
        {
            var dataTypes = new List<IDataType>();
            var propertyTypes = GetPropertyTypes(contentType);

            foreach (var propertyType in propertyTypes)
            {
                var dataType = _dataTypeService.GetDataType(propertyType.DataTypeId);

                if (dataType != null)
                {
                    dataTypes.Add(dataType);
                    var ncConfig = dataType.Configuration as NestedContentConfiguration;
                    if (ncConfig?.ContentTypes != null)
                    {
                        foreach (var ncContentType in ncConfig.ContentTypes)
                        {
                            var nestedContentType = _contentTypeService.Get(ncContentType.Alias);
                            if (nestedContentType != null)
                                dataTypes.AddRange(GetDataTypesInContentType(nestedContentType));
                        }
                    }
                }
            }

            return dataTypes;
        }

        public IEnumerable<int> GetComposedOf(IEnumerable<int> ids)
        {
            var contentTypes = new List<int>();

            foreach (int id in ids)
                contentTypes.AddRange(_contentTypeService.GetComposedOf(id).Select(x => x.Id));

            return contentTypes;
        }

        public IEnumerable<IPropertyType> GetPropertyTypes(IContentType contentType)
        {
            var propertyTypes = new List<IPropertyType>();
            propertyTypes.AddRange(contentType.PropertyTypes.Where(x => x.PropertyEditorAlias == PropertyEditors.Aliases.NestedContent));

            if (contentType.CompositionPropertyTypes.Any())
                propertyTypes.AddRange(contentType.CompositionPropertyTypes.Where(x => x.PropertyEditorAlias == PropertyEditors.Aliases.NestedContent));

            return propertyTypes;
        }

        public IEnumerable<IContentType> GetElementContentTypesFromDataType(IDataType dataType)
        {
            var contentTypes = new List<IContentType>();

            var usages = _dataTypeService.GetReferences(dataType.Id);

            foreach (var entityType in usages.Where(x => x.Key.EntityType == UmbracoObjectTypes.DocumentType.GetUdiType()))
            {
                var contentType = _contentTypeService.Get(((GuidUdi)entityType.Key).Guid);

                if (contentType != null && contentType.IsElement)
                    contentTypes.Add(contentType);
            }

            return contentTypes;
        }

        public bool HasBLContent(IContent item)
        {
            var ok = new List<bool>();

            var ncProperties = item.Properties.Where(x => x.PropertyType.PropertyEditorAlias == PropertyEditors.Aliases.NestedContent);

            foreach (var property in ncProperties)
            {
                var blProperty = item.Properties.FirstOrDefault(x => x.Alias == string.Format(GetAliasFormatting(), property.Alias));
                if (blProperty != null)
                {
                    if (property.PropertyType.VariesByCulture())
                    {
                        foreach (var language in item.AvailableCultures)
                        {
                            var ncValue = property.GetValue(language);
                            var blValue = blProperty.GetValue(language);

                            ok.Add(ncValue != null && blValue != null || ncValue == null && blValue == null);
                        }
                    }
                    else
                    {
                        var ncValue = property.GetValue();
                        var blValue = blProperty.GetValue();

                        ok.Add(ncValue != null && blValue != null || ncValue == null && blValue == null);
                    }
                }
                else
                {
                    ok.Add(false);
                }
            }

            return ok.All(x => x);
        }
    }
}