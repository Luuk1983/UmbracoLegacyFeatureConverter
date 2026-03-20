using Newtonsoft.Json;

namespace Umbraco.Community.LegacyFeatureConverter.Dtos.BlockList
{
    public class BlockListUdi
    {
        [JsonProperty("Umbraco.BlockList")]
        public List<Dictionary<string, string>> contentUdi { get; set; }

        [JsonIgnore]
        public List<Dictionary<string, string>> settingsUdi { get; set; }
        public BlockListUdi(List<Dictionary<string, string>> items, List<Dictionary<string, string>> settings)
        {
            contentUdi = items;
            settingsUdi = settings;
        }
    }
}
