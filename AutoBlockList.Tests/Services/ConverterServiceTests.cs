using AutoBlockList.Services;
using AutoBlockList.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;

namespace AutoBlockList.Tests.Services
{
    /// <summary>
    /// Tests for ConverterService.
    /// </summary>
    [TestClass]
    public class ConverterServiceTests
    {
        private Mock<IContentTypeService> _mockContentTypeService = null!;
        private Mock<ILogger<ConverterService>> _mockLogger = null!;
        private ConverterService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockContentTypeService = new Mock<IContentTypeService>();
            _mockLogger = new Mock<ILogger<ConverterService>>();
        }

        [TestMethod]
        public void GetAllConverters_ReturnsAllRegisteredConverters()
        {
            // Arrange
            var mockConverter1 = new Mock<AutoBlockList.Converters.IPropertyConverter>();
            var mockConverter2 = new Mock<AutoBlockList.Converters.IPropertyConverter>();
            var converters = new[] { mockConverter1.Object, mockConverter2.Object };
            
            _service = new ConverterService(converters, _mockContentTypeService.Object, _mockLogger.Object);

            // Act
            var result = _service.GetAllConverters();

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public void GetConverterByName_ReturnsCorrectConverter()
        {
            // Arrange
            var mockConverter1 = new Mock<AutoBlockList.Converters.IPropertyConverter>();
            mockConverter1.Setup(x => x.ConverterName).Returns("Converter One");
            
            var mockConverter2 = new Mock<AutoBlockList.Converters.IPropertyConverter>();
            mockConverter2.Setup(x => x.ConverterName).Returns("Converter Two");
            
            var converters = new[] { mockConverter1.Object, mockConverter2.Object };
            _service = new ConverterService(converters, _mockContentTypeService.Object, _mockLogger.Object);

            // Act
            var result = _service.GetConverterByName("Converter Two");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Converter Two", result.ConverterName);
        }

        [TestMethod]
        public void GetConverterByName_IsCaseInsensitive()
        {
            // Arrange
            var mockConverter = new Mock<AutoBlockList.Converters.IPropertyConverter>();
            mockConverter.Setup(x => x.ConverterName).Returns("Nested Content Converter");
            
            var converters = new[] { mockConverter.Object };
            _service = new ConverterService(converters, _mockContentTypeService.Object, _mockLogger.Object);

            // Act
            var result = _service.GetConverterByName("NESTED CONTENT CONVERTER");

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetConverterByName_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var mockConverter = new Mock<AutoBlockList.Converters.IPropertyConverter>();
            mockConverter.Setup(x => x.ConverterName).Returns("Existing Converter");
            
            var converters = new[] { mockConverter.Object };
            _service = new ConverterService(converters, _mockContentTypeService.Object, _mockLogger.Object);

            // Act
            var result = _service.GetConverterByName("Non-Existent Converter");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_ThrowsException_WhenConvertersIsNull()
        {
            // Act
            new ConverterService(null!, _mockContentTypeService.Object, _mockLogger.Object);
        }
    }
}
