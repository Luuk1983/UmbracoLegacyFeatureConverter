using Umbraco.Community.LegacyFeatureConverter.Models;

namespace Umbraco.Community.LegacyFeatureConverter.Tests.Models
{
    /// <summary>
    /// Tests for ConversionResult model calculations.
    /// </summary>
    [TestClass]
    public class ConversionResultTests
    {
        [TestMethod]
        public void SuccessCount_CalculatesCorrectly()
        {
            // Arrange
            var result = new ConversionResult();
            result.DocumentTypes.Add(new DocumentTypeConversionInfo { Success = true });
            result.DocumentTypes.Add(new DocumentTypeConversionInfo { Success = false });
            result.DataTypes.Add(new DataTypeConversionInfo { Success = true });
            result.ContentNodes.Add(new ContentConversionInfo { Success = true });
            result.ContentNodes.Add(new ContentConversionInfo { Success = true });

            // Act
            var successCount = result.SuccessCount;

            // Assert
            Assert.AreEqual(4, successCount, "Should count all successful operations");
        }

        [TestMethod]
        public void FailureCount_ExcludesSkippedItems()
        {
            // Arrange
            var result = new ConversionResult();
            result.DocumentTypes.Add(new DocumentTypeConversionInfo { Success = false, Skipped = false });
            result.DocumentTypes.Add(new DocumentTypeConversionInfo { Success = false, Skipped = true }); // Skipped
            result.DataTypes.Add(new DataTypeConversionInfo { Success = false, Skipped = false });

            // Act
            var failureCount = result.FailureCount;

            // Assert
            Assert.AreEqual(2, failureCount, "Should count only failed items, not skipped");
        }

        [TestMethod]
        public void SkippedCount_CountsOnlySkippedItems()
        {
            // Arrange
            var result = new ConversionResult();
            result.DocumentTypes.Add(new DocumentTypeConversionInfo { Skipped = true });
            result.DataTypes.Add(new DataTypeConversionInfo { Skipped = true });
            result.DataTypes.Add(new DataTypeConversionInfo { Skipped = false });
            result.ContentNodes.Add(new ContentConversionInfo { Skipped = true });

            // Act
            var skippedCount = result.SkippedCount;

            // Assert
            Assert.AreEqual(3, skippedCount, "Should count all skipped items");
        }

        [TestMethod]
        public void Duration_CalculatesCorrectly()
        {
            // Arrange
            var result = new ConversionResult
            {
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow
            };

            // Act
            var duration = result.Duration;

            // Assert
            Assert.IsNotNull(duration);
            Assert.IsTrue(duration.Value.TotalMinutes >= 4.9 && duration.Value.TotalMinutes <= 5.1,
                "Duration should be approximately 5 minutes");
        }

        [TestMethod]
        public void Duration_IsNull_WhenNotCompleted()
        {
            // Arrange
            var result = new ConversionResult
            {
                StartedAt = DateTime.UtcNow,
                CompletedAt = null
            };

            // Act
            var duration = result.Duration;

            // Assert
            Assert.IsNull(duration, "Duration should be null when conversion is not completed");
        }
    }
}
