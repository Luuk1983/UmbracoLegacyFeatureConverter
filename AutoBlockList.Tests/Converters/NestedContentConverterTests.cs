using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.LegacyFeatureConverter.Converters;
using Umbraco.Community.LegacyFeatureConverter.Dtos;
using Umbraco.Community.LegacyFeatureConverter.Services.interfaces;

namespace AutoBlockList.Tests.Converters
{
    /// <summary>
    /// Tests for NestedContentConverter.
    /// </summary>
    [TestClass]
    public class NestedContentConverterTests
    {
        private Mock<ILogger<NestedContentConverter>> _mockLogger = null!;
        private Mock<IDataTypeService> _mockDataTypeService = null!;
        private Mock<IContentTypeService> _mockContentTypeService = null!;
        private Mock<IContentService> _mockContentService = null!;
        private Mock<IConversionHistoryService> _mockHistoryService = null!;
        private Mock<IScopeProvider> _mockScopeProvider = null!;
        private Mock<IShortStringHelper> _mockShortStringHelper = null!;
        private Mock<IDataValueEditorFactory> _mockDataValueEditorFactory = null!;
        private Mock<PropertyEditorCollection> _mockPropertyEditorCollection = null!;
        private Mock<IConfigurationEditorJsonSerializer> _mockConfigSerializer = null!;
        private Mock<IOptions<AutoBlockListSettings>> _mockSettings = null!;
        private NestedContentConverter _converter = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<NestedContentConverter>>();
            _mockDataTypeService = new Mock<IDataTypeService>();
            _mockContentTypeService = new Mock<IContentTypeService>();
            _mockContentService = new Mock<IContentService>();
            _mockHistoryService = new Mock<IConversionHistoryService>();
            _mockScopeProvider = new Mock<IScopeProvider>();
            _mockShortStringHelper = new Mock<IShortStringHelper>();
            _mockDataValueEditorFactory = new Mock<IDataValueEditorFactory>();
            _mockPropertyEditorCollection = new Mock<PropertyEditorCollection>(new object[] { });
            _mockConfigSerializer = new Mock<IConfigurationEditorJsonSerializer>();
            
            var settings = new AutoBlockListSettings
            {
                BlockListEditorSize = "medium",
                NameFormatting = "[Block List] {0}",
                AliasFormatting = "{0}BL"
            };
            _mockSettings = new Mock<IOptions<AutoBlockListSettings>>();
            _mockSettings.Setup(x => x.Value).Returns(settings);

            _converter = new NestedContentConverter(
                _mockLogger.Object,
                _mockDataTypeService.Object,
                _mockContentTypeService.Object,
                _mockContentService.Object,
                _mockHistoryService.Object,
                _mockScopeProvider.Object,
                _mockShortStringHelper.Object,
                _mockDataValueEditorFactory.Object,
                _mockPropertyEditorCollection.Object,
                _mockConfigSerializer.Object,
                _mockSettings.Object
            );
        }

        [TestMethod]
        public void ConverterName_ReturnsCorrectName()
        {
            // Assert
            Assert.AreEqual("Nested Content to Block List", _converter.ConverterName);
        }

        [TestMethod]
        public void SourcePropertyEditorAliases_ContainsNestedContent()
        {
            // Assert
            Assert.IsTrue(_converter.SourcePropertyEditorAliases.Contains("Umbraco.NestedContent"));
        }

        [TestMethod]
        public void TargetPropertyEditorAlias_IsBlockList()
        {
            // Assert
            Assert.AreEqual("Umbraco.BlockList", _converter.TargetPropertyEditorAlias);
        }

        // TODO: Add integration tests for actual conversion logic
        // These would require mocking more complex Umbraco services or using test database
    }
}
