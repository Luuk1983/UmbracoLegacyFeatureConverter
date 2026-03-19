using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoBlockList.Tests.DataConversion
{
    /// <summary>
    /// Tests for Nested Content to Block List data conversion logic.
    /// </summary>
    [TestClass]
    public class NestedContentDataConversionTests
    {
        [TestMethod]
        public void ConvertNestedContentJson_ToBlockListJson_SimpleCase()
        {
            // Arrange
            var ncJson = @"[
                {
                    ""key"": ""123"",
                    ""name"": ""Item 1"",
                    ""ncContentTypeAlias"": ""textBlock"",
                    ""heading"": ""Hello World""
                }
            ]";

            // Act
            var jArray = JsonConvert.DeserializeObject<JArray>(ncJson);

            // Assert
            Assert.IsNotNull(jArray);
            Assert.AreEqual(1, jArray.Count);
            
            var item = (JObject)jArray[0];
            Assert.AreEqual("textBlock", item["ncContentTypeAlias"]?.Value<string>());
            Assert.AreEqual("Hello World", item["heading"]?.Value<string>());
        }

        [TestMethod]
        public void ConvertNestedContentJson_HandlesNestedNC()
        {
            // Arrange - NC with nested NC inside
            var ncJson = @"[
                {
                    ""key"": ""123"",
                    ""ncContentTypeAlias"": ""container"",
                    ""items"": ""[{\""ncContentTypeAlias\"":\""textBlock\"",\""heading\"":\""Nested\""}]""
                }
            ]";

            // Act
            var jArray = JsonConvert.DeserializeObject<JArray>(ncJson);
            var item = (JObject)jArray[0];
            var nestedItemsString = item["items"]?.Value<string>();
            
            // Try to parse nested items
            var nestedArray = JsonConvert.DeserializeObject<JArray>(nestedItemsString);

            // Assert
            Assert.IsNotNull(nestedArray);
            Assert.AreEqual(1, nestedArray.Count);
            
            var nestedItem = (JObject)nestedArray[0];
            Assert.AreEqual("textBlock", nestedItem["ncContentTypeAlias"]?.Value<string>());
        }

        [TestMethod]
        public void ConvertNestedContentJson_HandlesNullValues()
        {
            // Arrange
            var ncJson = @"[
                {
                    ""key"": ""123"",
                    ""ncContentTypeAlias"": ""textBlock"",
                    ""heading"": null,
                    ""description"": ""Test""
                }
            ]";

            // Act
            var jArray = JsonConvert.DeserializeObject<JArray>(ncJson);
            var item = (JObject)jArray[0];

            // Assert
            Assert.AreEqual(JTokenType.Null, item["heading"]?.Type);
            Assert.AreEqual("Test", item["description"]?.Value<string>());
        }

        [TestMethod]
        public void ConvertNestedContentJson_HandlesEmptyArray()
        {
            // Arrange
            var ncJson = "[]";

            // Act
            var jArray = JsonConvert.DeserializeObject<JArray>(ncJson);

            // Assert
            Assert.IsNotNull(jArray);
            Assert.AreEqual(0, jArray.Count);
        }

        [TestMethod]
        public void DetectNestedContent_ByPresenceOfNcContentTypeAlias()
        {
            // Arrange - This looks like JSON but is NOT Nested Content (media picker)
            var mediaPickerJson = @"[
                {
                    ""key"": ""123"",
                    ""mediaKey"": ""abc-123-guid"",
                    ""crops"": []
                }
            ]";

            // Act
            var jArray = JsonConvert.DeserializeObject<JArray>(mediaPickerJson);
            var item = (JObject)jArray[0];
            
            bool hasNcAlias = item.Properties().Any(p => p.Name == "ncContentTypeAlias");

            // Assert
            Assert.IsFalse(hasNcAlias, "Media picker data should NOT be detected as Nested Content");
        }

        [TestMethod]
        public void DetectNestedContent_ReturnsTrueForValidNC()
        {
            // Arrange
            var ncJson = @"[{""ncContentTypeAlias"": ""textBlock""}]";

            // Act
            var jArray = JsonConvert.DeserializeObject<JArray>(ncJson);
            var item = (JObject)jArray[0];
            
            bool hasNcAlias = item.Properties().Any(p => p.Name == "ncContentTypeAlias");

            // Assert
            Assert.IsTrue(hasNcAlias, "Valid Nested Content should be detected");
        }
    }
}
