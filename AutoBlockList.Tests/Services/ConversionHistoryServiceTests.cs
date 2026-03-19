using AutoBlockList.Services;
using AutoBlockList.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Scoping;

namespace AutoBlockList.Tests.Services
{
    /// <summary>
    /// Tests for ConversionHistoryService.
    /// Note: These are unit tests using mocked dependencies.
    /// Integration tests would test against a real database.
    /// </summary>
    [TestClass]
    public class ConversionHistoryServiceTests
    {
        private Mock<IScopeProvider> _mockScopeProvider = null!;
        private Mock<ILogger<ConversionHistoryService>> _mockLogger = null!;
        private ConversionHistoryService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockScopeProvider = new Mock<IScopeProvider>();
            _mockLogger = new Mock<ILogger<ConversionHistoryService>>();
            _service = new ConversionHistoryService(_mockScopeProvider.Object, _mockLogger.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_ThrowsException_WhenScopeProviderIsNull()
        {
            // Act
            new ConversionHistoryService(null!, _mockLogger.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_ThrowsException_WhenLoggerIsNull()
        {
            // Act
            new ConversionHistoryService(_mockScopeProvider.Object, null!);
        }

        // TODO: Add more tests once we have integration test infrastructure
        // These would test actual database operations
    }
}
