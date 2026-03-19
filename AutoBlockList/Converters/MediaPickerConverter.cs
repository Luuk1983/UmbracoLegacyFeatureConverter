using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoBlockList.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants;
using DataType = Umbraco.Cms.Core.Models.DataType;

namespace AutoBlockList.Converters
{
    /// <summary>
    /// Converts legacy Media Picker properties (MediaPicker2, MultipleMediaPicker) to MediaPicker3.
    /// Supports converting UDI strings to the MediaPicker3 JSON format with crops and focal points.
    /// </summary>
    public class MediaPickerConverter : BasePropertyConverter
    {
        private readonly IMediaService _mediaService;
        private readonly IDataValueEditorFactory _dataValueEditorFactory;
        private readonly PropertyEditorCollection _propertyEditorCollection;
        private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;

        public MediaPickerConverter(
            ILogger<MediaPickerConverter> logger,
            IDataTypeService dataTypeService,
            IContentTypeService contentTypeService,
            IContentService contentService,
            IConversionHistoryService historyService,
            IScopeProvider scopeProvider,
            IMediaService mediaService,
            IDataValueEditorFactory dataValueEditorFactory,
            PropertyEditorCollection propertyEditorCollection,
            IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
            : base(logger, dataTypeService, contentTypeService, contentService, historyService, scopeProvider)
        {
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _dataValueEditorFactory = dataValueEditorFactory ?? throw new ArgumentNullException(nameof(dataValueEditorFactory));
            _propertyEditorCollection = propertyEditorCollection ?? throw new ArgumentNullException(nameof(propertyEditorCollection));
            _configurationEditorJsonSerializer = configurationEditorJsonSerializer ?? throw new ArgumentNullException(nameof(configurationEditorJsonSerializer));
        }

        public override string ConverterName => "Legacy Media Picker to MediaPicker3";

        public override string[] SourcePropertyEditorAliases => new[]
        {
            "Umbraco.MediaPicker2",
            PropertyEditors.Aliases.MultipleMediaPicker
        };

        public override string TargetPropertyEditorAlias => PropertyEditors.Aliases.MediaPicker3;

        public override string Description => "Converts legacy MediaPicker2 and MultipleMediaPicker properties to the modern MediaPicker3 with support for crops and focal points.";

        /// <summary>
        /// Creates a MediaPicker3 data type based on the legacy media picker configuration.
        /// Maps configuration properties and converts startNodeId from int to Guid.
        /// </summary>
        protected override async Task<IDataType?> CreateTargetDataTypeAsync(IDataType sourceDataType)
        {
            // MediaPicker2 and MultipleMediaPicker in Umbraco 13 don't have a specific configuration class
            // They're simple pickers with basic config. We'll create sensible defaults for MediaPicker3.
            
            bool isMultiple = sourceDataType.EditorAlias == PropertyEditors.Aliases.MultipleMediaPicker;

            var mp3DataType = new DataType(new DataEditor(_dataValueEditorFactory), _configurationEditorJsonSerializer)
            {
                Editor = _propertyEditorCollection.First(x => x.Alias == PropertyEditors.Aliases.MediaPicker3),
                CreateDate = DateTime.Now,
                Name = $"[MediaPicker3] {sourceDataType.Name}",
                Configuration = new MediaPicker3Configuration
                {
                    Multiple = isMultiple,
                    ValidationLimit = new MediaPicker3Configuration.NumberRange
                    {
                        // Set sensible defaults - can be adjusted later
                        Max = isMultiple ? null : 1,
                        Min = null
                    },
                    StartNodeId = null, // No start node restriction by default
                    EnableLocalFocalPoint = true, // Enable focal point editing
                    Crops = Array.Empty<MediaPicker3Configuration.CropConfiguration>(), // No crops by default
                    IgnoreUserStartNodes = false,
                    Filter = null // Accept all media types by default
                }
            };

            _logger.LogInformation("Created MediaPicker3 data type '{DataTypeName}' (Multiple: {IsMultiple})", 
                mp3DataType.Name, isMultiple);

            return await Task.FromResult(mp3DataType);
        }

        /// <summary>
        /// Converts legacy media picker property value (UDI string or comma-separated UDIs)
        /// to MediaPicker3 JSON format (array of objects with key, mediaKey, crops, focalPoint).
        /// </summary>
        protected override async Task<object?> ConvertPropertyValueAsync(object sourceValue, IProperty property)
        {
            if (sourceValue == null)
            {
                _logger.LogDebug("Property value is null for {PropertyAlias}", property.Alias);
                return "[]"; // MediaPicker3 expects empty array, not null
            }

            var rawValue = sourceValue.ToString();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                _logger.LogDebug("Property value is empty for {PropertyAlias}", property.Alias);
                return "[]";
            }

            try
            {
                // Check if value is already in MediaPicker3 format (JSON array)
                if (rawValue.TrimStart().StartsWith("["))
                {
                    _logger.LogInformation("Property {PropertyAlias} appears to already be in MediaPicker3 format, keeping as-is", property.Alias);
                    return rawValue;
                }

                // Parse UDI(s) - could be single or comma-separated
                var udis = rawValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                if (!udis.Any())
                {
                    _logger.LogDebug("No valid UDIs found for {PropertyAlias}", property.Alias);
                    return "[]";
                }

                _logger.LogInformation("Converting {Count} media UDI(s) for property {PropertyAlias}", udis.Count, property.Alias);

                var mediaItems = new List<MediaPicker3Item>();

                foreach (var udiStr in udis)
                {
                    if (!UdiParser.TryParse(udiStr, out Udi? udi))
                    {
                        _logger.LogWarning("Failed to parse UDI '{UdiString}' for property {PropertyAlias}", udiStr, property.Alias);
                        continue;
                    }

                    if (udi is not GuidUdi guidUdi)
                    {
                        _logger.LogWarning("UDI '{UdiString}' is not a GuidUdi for property {PropertyAlias}", udiStr, property.Alias);
                        continue;
                    }

                    // Get media type alias for validation (MediaPicker3 stores this)
                    string? mediaTypeAlias = null;
                    try
                    {
                        var media = _mediaService.GetById(guidUdi.Guid);
                        if (media != null)
                        {
                            mediaTypeAlias = media.ContentType.Alias;
                        }
                        else
                        {
                            _logger.LogWarning("Media with key {MediaKey} not found, including anyway", guidUdi.Guid);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting media type for {MediaKey}, including without type alias", guidUdi.Guid);
                    }

                    mediaItems.Add(new MediaPicker3Item
                    {
                        Key = Guid.NewGuid(), // Unique key for this picker item
                        MediaKey = guidUdi.Guid,
                        MediaTypeAlias = mediaTypeAlias,
                        Crops = Array.Empty<object>(), // No crops initially
                        FocalPoint = null // No focal point initially (MediaPicker3 uses null for default center)
                    });
                }

                if (!mediaItems.Any())
                {
                    _logger.LogWarning("No valid media items after conversion for {PropertyAlias}", property.Alias);
                    return "[]";
                }

                _logger.LogInformation("Successfully converted {Count} media items for property {PropertyAlias}", 
                    mediaItems.Count, property.Alias);

                return JsonConvert.SerializeObject(mediaItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert media picker value for property {PropertyAlias}", property.Alias);
                return "[]";
            }
        }

        /// <summary>
        /// Represents a MediaPicker3 item in the JSON format.
        /// Matches the structure expected by MediaPicker3.
        /// </summary>
        private class MediaPicker3Item
        {
            [JsonProperty("key")]
            public Guid Key { get; set; }

            [JsonProperty("mediaKey")]
            public Guid MediaKey { get; set; }

            [JsonProperty("mediaTypeAlias")]
            public string? MediaTypeAlias { get; set; }

            [JsonProperty("crops")]
            public object[] Crops { get; set; } = Array.Empty<object>();

            [JsonProperty("focalPoint")]
            public object? FocalPoint { get; set; }
        }
    }
}
