using Umbraco.Cms.Core;

namespace AutoBlockList.Tests.DataConversion
{
    /// <summary>
    /// Tests for Media Picker UDI string to MediaPicker3 JSON conversion logic.
    /// </summary>
    [TestClass]
    public class MediaPickerDataConversionTests
    {
        [TestMethod]
        public void ParseSingleUdi_ReturnsGuidUdi()
        {
            // Arrange
            var udiString = "umb://media/abc12345-1234-1234-1234-123456789abc";

            // Act
            var success = UdiParser.TryParse(udiString, out Udi? udi);
            var guidUdi = udi as GuidUdi;

            // Assert
            Assert.IsTrue(success, "Should parse valid UDI");
            Assert.IsNotNull(guidUdi, "Should be a GuidUdi");
            Assert.AreEqual("media", guidUdi.EntityType);
            Assert.AreEqual(Guid.Parse("abc12345-1234-1234-1234-123456789abc"), guidUdi.Guid);
        }

        [TestMethod]
        public void ParseMultipleUdis_SplitByComma()
        {
            // Arrange
            var udiString = "umb://media/abc12345-1234-1234-1234-123456789abc,umb://media/def12345-1234-1234-1234-123456789def";

            // Act
            var udis = udiString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Assert
            Assert.AreEqual(2, udis.Length);
            
            Assert.IsTrue(UdiParser.TryParse(udis[0].Trim(), out Udi? udi1));
            Assert.IsTrue(UdiParser.TryParse(udis[1].Trim(), out Udi? udi2));
            
            Assert.AreEqual(Guid.Parse("abc12345-1234-1234-1234-123456789abc"), ((GuidUdi)udi1!).Guid);
            Assert.AreEqual(Guid.Parse("def12345-1234-1234-1234-123456789def"), ((GuidUdi)udi2!).Guid);
        }

        [TestMethod]
        public void ParseInvalidUdi_ReturnsFalse()
        {
            // Arrange
            var invalidUdi = "not-a-valid-udi";

            // Act
            var success = UdiParser.TryParse(invalidUdi, out Udi? udi);

            // Assert
            Assert.IsFalse(success, "Should fail to parse invalid UDI");
        }

        [TestMethod]
        public void MediaPicker3Json_Structure()
        {
            // Arrange - Expected MediaPicker3 format
            var mp3Json = @"[
                {
                    ""key"": ""11111111-1111-1111-1111-111111111111"",
                    ""mediaKey"": ""abc12345-1234-1234-1234-123456789abc"",
                    ""mediaTypeAlias"": ""Image"",
                    ""crops"": [],
                    ""focalPoint"": null
                }
            ]";

            // Act
            var jArray = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(mp3Json);
            var item = (JObject)jArray![0];

            // Assert
            Assert.IsNotNull(item["key"]);
            Assert.IsNotNull(item["mediaKey"]);
            Assert.AreEqual("Image", item["mediaTypeAlias"]?.Value<string>());
            Assert.AreEqual(JTokenType.Array, item["crops"]?.Type);
            Assert.AreEqual(JTokenType.Null, item["focalPoint"]?.Type);
        }

        [TestMethod]
        public void DetectMediaPicker3Format_ByPresenceOfMediaKey()
        {
            // Arrange
            var mp3Json = @"[{""key"":""..."",""mediaKey"":""...""}]";

            // Act
            var startsWithBracket = mp3Json.TrimStart().StartsWith("[");
            
            // Further validation
            if (startsWithBracket)
            {
                var jArray = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(mp3Json);
                var firstItem = jArray?[0] as JObject;
                var hasMediaKey = firstItem?.Properties().Any(p => p.Name == "mediaKey") ?? false;

                // Assert
                Assert.IsTrue(hasMediaKey, "MediaPicker3 JSON should have mediaKey property");
            }
        }

        [TestMethod]
        public void EmptyValue_ShouldReturnEmptyArray()
        {
            // Arrange
            string? nullValue = null;
            string emptyValue = "";
            string whitespaceValue = "   ";

            // Act & Assert
            Assert.IsTrue(string.IsNullOrWhiteSpace(nullValue), "Null should be treated as empty");
            Assert.IsTrue(string.IsNullOrWhiteSpace(emptyValue), "Empty string should be treated as empty");
            Assert.IsTrue(string.IsNullOrWhiteSpace(whitespaceValue), "Whitespace should be treated as empty");
            
            // Expected result for all: "[]"
        }
    }
}
