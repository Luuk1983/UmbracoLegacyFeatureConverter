using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.LegacyFeatureConverter.Converters;
using Umbraco.Community.LegacyFeatureConverter.Services.interfaces;

namespace AutoBlockList.Tests.Converters
{
    /// <summary>
    /// Tests for MediaPickerConverter.
    /// </summary>
    [TestClass]
    public class MediaPickerConverterTests
    {
        private Mock<ILogger<MediaPickerConverter>> _mockLogger = null!;
        private Mock<IDataTypeService> _mockDataTypeService = null!;
        private Mock<IContentTypeService> _mockContentTypeService = null!;
        private Mock<IContentService> _mockContentService = null!;
        private Mock<IConversionHistoryService> _mockHistoryService = null!;
        private Mock<IScopeProvider> _mockScopeProvider = null!;
        private Mock<IMediaService> _mockMediaService = null!;
        private Mock<IDataValueEditorFactory> _mockDataValueEditorFactory = null!;
        private Mock<PropertyEditorCollection> _mockPropertyEditorCollection = null!;
        private Mock<IConfigurationEditorJsonSerializer> _mockConfigSerializer = null!;
        private MediaPickerConverter _converter = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<MediaPickerConverter>>();
            _mockDataTypeService = new Mock<IDataTypeService>();
            _mockContentTypeService = new Mock<IContentTypeService>();
            _mockContentService = new Mock<IContentService>();
            _mockHistoryService = new Mock<IConversionHistoryService>();
            _mockScopeProvider = new Mock<IScopeProvider>();
            _mockMediaService = new Mock<IMediaService>();
            _mockDataValueEditorFactory = new Mock<IDataValueEditorFactory>();
            _mockPropertyEditorCollection = new Mock<PropertyEditorCollection>(new object[] { });
            _mockConfigSerializer = new Mock<IConfigurationEditorJsonSerializer>();

            _converter = new MediaPickerConverter(
                _mockLogger.Object,
                _mockDataTypeService.Object,
                _mockContentTypeService.Object,
                _mockContentService.Object,
                _mockHistoryService.Object,
                _mockScopeProvider.Object,
                _mockMediaService.Object,
                _mockDataValueEditorFactory.Object,
                _mockPropertyEditorCollection.Object,
                _mockConfigSerializer.Object
            );
        }

        [TestMethod]
        public void ConverterName_ReturnsCorrectName()
        {
            // Assert
            Assert.AreEqual("Legacy Media Picker to MediaPicker3", _converter.ConverterName);
        }

        [TestMethod]
        public void SourcePropertyEditorAliases_ContainsLegacyMediaPickers()
        {
            // Assert
            Assert.IsTrue(_converter.SourcePropertyEditorAliases.Contains("Umbraco.MediaPicker2"),
                "Should contain MediaPicker2");
            Assert.IsTrue(_converter.SourcePropertyEditorAliases.Contains("Umbraco.MultipleMediaPicker"),
                "Should contain MultipleMediaPicker");
        }

        [TestMethod]
        public void TargetPropertyEditorAlias_IsMediaPicker3()
        {
            // Assert
            Assert.AreEqual("Umbraco.MediaPicker3", _converter.TargetPropertyEditorAlias);
        }

        [TestMethod]
        public void SourcePropertyEditorAliases_Count()
        {
            // Assert
            Assert.AreEqual(2, _converter.SourcePropertyEditorAliases.Length,
                "Should support exactly 2 legacy media picker types");
        }

        // TODO: Add integration tests for UDI conversion logic
        // These would test converting single and multiple UDI strings to MediaPicker3 JSON format
    }
}
